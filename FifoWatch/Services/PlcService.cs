using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FifoWatch.Models;
using S7CommPlusDriver;
using S7CommPlusDriver.ClientApi;

namespace FifoWatch.Services
{
    public class PlcService
    {
        private static readonly string LogPath = Path.Combine(
            Path.GetTempPath(), "fifowatch.log");

        private static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {msg}\r\n"); }
            catch { }
        }

        private readonly System.Threading.SemaphoreSlim _connLock = new System.Threading.SemaphoreSlim(1, 1);

        private S7CommPlusConnection _conn;
        private List<VarInfo> _varInfoList = new List<VarInfo>();

        // Per-tag cache for symbol-based array expansion (keyed on array tag name)
        private readonly Dictionary<string, List<VarInfo>> _arrayVarInfoCache = new Dictionary<string, List<VarInfo>>();

        public bool IsConnected { get; private set; }

        public int Connect(string address, string username, string password)
        {
            _conn = new S7CommPlusConnection();
            int res = _conn.Connect(address, password, username);
            IsConnected = (res == 0);
            Log($"Connect result={res} IsConnected={IsConnected}");
            return res;
        }

        public void Disconnect()
        {
            if (_conn != null)
            {
                _conn.Disconnect();
                _conn = null;
            }
            IsConnected = false;
            _varInfoList.Clear();
            _arrayVarInfoCache.Clear();
        }

        // --- Tree-browse API (no full Browse() required) ---

        public List<S7CommPlusConnection.DatablockInfo> GetDatablocks()
        {
            List<S7CommPlusConnection.DatablockInfo> list;
            _conn.GetListOfDatablocks(out list);
            return list ?? new List<S7CommPlusConnection.DatablockInfo>();
        }

        public PObject GetTypeInfo(uint relId)
        {
            try { return _conn.getTypeInfoByRelId(relId); }
            catch { return null; }
        }

        // Fetches the full PLC type-info container and caches all types locally.
        // Called once when a DB can't be expanded via the normal per-relid path.
        public bool PreloadTypeInfoCache()
        {
            List<VarInfo> _;
            return _conn.Browse(out _) == 0;
        }

        // Resolves a symbolic PLC name to a VarInfo using the driver's symbol resolver.
        public VarInfo GetVarInfoBySymbol(string symbol)
        {
            PlcTag tag = _conn.getPlcTagBySymbol(symbol);
            if (tag == null) return null;
            var vi = new VarInfo();
            vi.Name = symbol;
            vi.AccessSequence = tag.Address.GetAccessString();
            vi.Softdatatype = tag.Datatype;
            return vi;
        }

        // --- Legacy flat-browse API (optional) ---

        public int Browse(out List<VarInfo> varInfoList)
        {
            varInfoList = new List<VarInfo>();
            if (!IsConnected)
                return -1;

            int res = _conn.Browse(out varInfoList);
            if (res == 0)
                _varInfoList = varInfoList;

            return res;
        }

        public List<VarInfo> GetCachedVarInfoList() => _varInfoList;

        // --- FIFO reading ---

        // Returns a Task that completes only once any in-progress ReadFifo call has finished.
        // Call this (with await) before opening BrowseForm to avoid concurrent driver access.
        public async System.Threading.Tasks.Task WaitForIdleAsync()
        {
            await _connLock.WaitAsync();
            _connLock.Release();
        }

        public List<FifoEntry> ReadFifo(FifoDefinition def, out int head, out int tail, out int count, out int maxRecords)
        {
            head = -1;
            tail = -1;
            count = -1;
            maxRecords = -1;

            if (!IsConnected || !def.IsValid)
                return null;

            _connLock.Wait();
            try
            {

            // --- Step 1: Read pointer tags in their own call so labels always update ---
            var ptrTags = new List<PlcTag>();
            int headIdx = -1, tailIdx = -1, countIdx = -1, maxRecordsIdx = -1;

            PlcTag MakePtr(VarInfo vi)
            {
                if (vi == null || string.IsNullOrEmpty(vi.AccessSequence)) return null;
                return PlcTags.TagFactory(vi.Name, new ItemAddress(vi.AccessSequence), vi.Softdatatype);
            }

            PlcTag pt;
            if ((pt = MakePtr(def.HeadTag))       != null) { headIdx       = ptrTags.Count; ptrTags.Add(pt); }
            if ((pt = MakePtr(def.TailTag))        != null) { tailIdx       = ptrTags.Count; ptrTags.Add(pt); }
            if ((pt = MakePtr(def.CountTag))       != null) { countIdx      = ptrTags.Count; ptrTags.Add(pt); }
            if ((pt = MakePtr(def.MaxRecordsTag))  != null) { maxRecordsIdx = ptrTags.Count; ptrTags.Add(pt); }

            Log($"PtrTags count={ptrTags.Count} headIdx={headIdx} tailIdx={tailIdx} countIdx={countIdx} maxRecordsIdx={maxRecordsIdx}");
            if (ptrTags.Count > 0)
            {
                int ptrRes = _conn.ReadTags(ptrTags);
                Log($"ReadTags(ptr) result={ptrRes}");
                for (int pi = 0; pi < ptrTags.Count; pi++)
                {
                    var pt2 = ptrTags[pi];
                    Log($"  ptr[{pi}] quality=0x{pt2.Quality:X2} value={pt2}");
                }
                head       = headIdx       >= 0 ? TagToInt(ptrTags[headIdx])       : -1;
                tail       = tailIdx       >= 0 ? TagToInt(ptrTags[tailIdx])       : -1;
                count      = countIdx      >= 0 ? TagToInt(ptrTags[countIdx])      : -1;
                maxRecords = maxRecordsIdx >= 0 ? TagToInt(ptrTags[maxRecordsIdx]) : -1;
                Log($"Parsed: head={head} tail={tail} count={count} maxRecords={maxRecords}");
            }

            // --- Step 2: Resolve array variable list ---
            string arrayPrefix = GetArrayPrefix(def.ArrayTag.Name);
            List<VarInfo> arrayVars;

            if (string.IsNullOrEmpty(def.ArrayTag.AccessSequence))
            {
                arrayVars = BuildArrayVarInfosFromSymbol(def.ArrayTag.Name, def.ArrayTag.Name);
                if (arrayVars.Count > 0)
                    arrayPrefix = OuterArrayPrefix(arrayVars[0].Name);
            }
            else if (arrayPrefix == def.ArrayTag.Name)
            {
                arrayVars = new List<VarInfo> { def.ArrayTag };
            }
            else if (_varInfoList.Count > 0)
            {
                arrayVars = _varInfoList
                    .Where(v => v.Name.StartsWith(arrayPrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(v => ExtractArrayIndex(v.Name, arrayPrefix))
                    .ThenBy(v => v.Name)
                    .ToList();
                if (arrayVars.Count == 0)
                    arrayVars = new List<VarInfo> { def.ArrayTag };
            }
            else
            {
                arrayVars = BuildArrayVarInfosFromSymbol(def.ArrayTag.Name, arrayPrefix);
                if (arrayVars.Count == 0)
                    arrayVars = new List<VarInfo> { def.ArrayTag };
            }

            // --- Step 3: Read array tags ---
            var arrayTags = new List<PlcTag>();
            foreach (var v in arrayVars)
            {
                if (string.IsNullOrEmpty(v.AccessSequence)) continue;
                var t = PlcTags.TagFactory(v.Name, new ItemAddress(v.AccessSequence), v.Softdatatype);
                if (t != null) arrayTags.Add(t);
            }

            var entries = new List<FifoEntry>();
            var entrySoftdatatypes = new List<uint>();
            if (arrayTags.Count > 0 && _conn.ReadTags(arrayTags) == 0)
            {
                // Capacity = number of distinct array indices (correct for both scalar and struct arrays)
                int capacity = arrayVars
                    .Select(v => ExtractArrayIndex(v.Name, arrayPrefix))
                    .Distinct()
                    .Count();
                if (capacity == 0) capacity = arrayTags.Count;

                Log($"Window: head={head} tail={tail} stored={count} capacity={capacity} arrayTags={arrayTags.Count} arrayVars={arrayVars.Count}");

                for (int i = 0; i < arrayTags.Count; i++)
                {
                    var tag    = arrayTags[i];
                    var varInfo = arrayVars[i < arrayVars.Count ? i : arrayVars.Count - 1];
                    int arrayIndex = ExtractArrayIndex(varInfo.Name, arrayPrefix);

                    if (head >= 0 && tail >= 0)
                    {
                        if (!IsInFifoWindow(arrayIndex, head, tail, capacity, count))
                            continue;
                    }

                    entries.Add(new FifoEntry
                    {
                        ArrayIndex = arrayIndex,
                        Variable   = varInfo.Name,
                        Value      = (tag.Quality & PlcTagQC.TAG_QUALITY_MASK) == PlcTagQC.TAG_QUALITY_GOOD
                                     ? TagValueString(tag) : "(no data)",
                    });
                    entrySoftdatatypes.Add(varInfo.Softdatatype);
                }

                Log($"After filter: entries={entries.Count} (before collapse)");

                // When FIFO is empty (count=0, head==tail), surface the last-written record
                // at index (tail-1) so the caller can seed its stale "last seen" display.
                if (entries.Count == 0 && count == 0 && head == tail && head >= 0)
                {
                    int wrapSize = maxRecords > 0 ? maxRecords : capacity;
                    if (wrapSize > 0)
                    {
                        int lastIdx = (tail - 1 + wrapSize) % wrapSize;
                        for (int i = 0; i < arrayTags.Count; i++)
                        {
                            var vi2 = arrayVars[i < arrayVars.Count ? i : arrayVars.Count - 1];
                            if (ExtractArrayIndex(vi2.Name, arrayPrefix) != lastIdx) continue;
                            entries.Add(new FifoEntry
                            {
                                ArrayIndex = lastIdx,
                                Variable   = vi2.Name,
                                Value      = (arrayTags[i].Quality & PlcTagQC.TAG_QUALITY_MASK) == PlcTagQC.TAG_QUALITY_GOOD
                                             ? TagValueString(arrayTags[i]) : "(no data)",
                            });
                            entrySoftdatatypes.Add(vi2.Softdatatype);
                        }
                        Log($"Empty FIFO: surfaced last-written at index {lastIdx}, entries={entries.Count}");
                    }
                }
            }

            entries = CollapseByteArrays(entries, entrySoftdatatypes);

            return entries;

            } // try
            finally { _connLock.Release(); }
        }

        // Searches for standard FIFO header fields near the selected array variable.
        // Checks the parent struct level of the array first, then looks one level deeper
        // into any struct-typed siblings (e.g. a "Header" UDT next to the array).
        public Dictionary<string, VarInfo> AutoDetectFifoHeader(string arrayVarName, out string diagnostics)
        {
            var result = new Dictionary<string, VarInfo>(StringComparer.OrdinalIgnoreCase);
            var diag   = new System.Text.StringBuilder();

            if (string.IsNullOrEmpty(arrayVarName))
            {
                diagnostics = "Array tag name is empty.";
                return result;
            }

            // parentPath = path to the struct that contains the array field.
            // e.g. "DB_Host_Interface".Send.CounterUpdate.Data[0].Location
            //   →  "DB_Host_Interface".Send.CounterUpdate.
            // e.g. "DB_CounterFifo".Buffer[0].Field
            //   →  "DB_CounterFifo".
            string parentPath = ExtractParentPath(arrayVarName);
            if (parentPath == null)
            {
                diagnostics = $"Could not determine parent path from: {arrayVarName}";
                return result;
            }

            diag.AppendLine($"Searching at: {parentPath}");
            string[] fields = { "NextIndexToRead", "NextIndexToWrite", "RecordsStored", "MaxNrOfRecords" };

            if (!IsConnected)
            {
                diagnostics = "Not connected.";
                return result;
            }

            // Pass 1: try fields directly at the parent struct level
            foreach (var field in fields)
            {
                if (result.ContainsKey(field)) continue;
                string sym = parentPath + field;
                TryResolveFieldSymbol(sym, field, result, diag);
            }

            // Pass 2: look inside struct-typed siblings of the array at the parent level
            // (handles e.g. a Header UDT that lives next to the array)
            if (result.Count < fields.Length)
            {
                diag.AppendLine("Checking struct siblings at parent level...");
                try
                {
                    FindFieldsInStructSiblings(parentPath, fields, result, diag);
                }
                catch (Exception ex)
                {
                    diag.AppendLine($"  Sibling search error: {ex.Message}");
                }
            }

            diagnostics = diag.ToString().TrimEnd();
            return result;
        }

        // Tries to resolve a single field symbol; adds to result if successful.
        private void TryResolveFieldSymbol(string sym, string fieldKey,
            Dictionary<string, VarInfo> result, System.Text.StringBuilder diag)
        {
            if (result.ContainsKey(fieldKey)) return;
            try
            {
                PlcTag tag = _conn.getPlcTagBySymbol(sym);
                if (tag != null)
                {
                    var vi = new VarInfo();
                    vi.Name = sym;
                    vi.AccessSequence = tag.Address.GetAccessString();
                    vi.Softdatatype = tag.Datatype;
                    result[fieldKey] = vi;
                    diag.AppendLine($"  ✓ {sym}");
                }
                else
                {
                    diag.AppendLine($"  ✗ {sym}");
                }
            }
            catch (Exception ex)
            {
                diag.AppendLine($"  ! {sym}: {ex.Message}");
            }
        }

        // Navigates type info at parentPath, finds struct-typed siblings of the array,
        // and checks inside each for the requested field names.
        private void FindFieldsInStructSiblings(string parentPath, string[] fieldNames,
            Dictionary<string, VarInfo> result, System.Text.StringBuilder diag)
        {
            PObject parentObj = NavigateToTypeInfo(parentPath.TrimEnd('.'));
            if (parentObj?.VarnameList == null) return;

            for (int i = 0; i < parentObj.VarnameList.Names.Count; i++)
            {
                string sibName = parentObj.VarnameList.Names[i];
                var sibVte     = parentObj.VartypeList.Elements[i];

                // Only consider non-array struct fields
                if (sibVte.OffsetInfoType.Is1Dim() || sibVte.OffsetInfoType.IsMDim()) continue;
                if (!sibVte.OffsetInfoType.HasRelation()) continue;

                string sibPath = parentPath + sibName + ".";
                diag.AppendLine($"  Checking sibling struct: {sibPath}");

                var sibRel = (IOffsetInfoType_Relation)sibVte.OffsetInfoType;
                PObject sibObj = _conn.getTypeInfoByRelId(sibRel.GetRelationId());
                if (sibObj?.VarnameList == null) continue;

                foreach (var field in fieldNames)
                {
                    if (result.ContainsKey(field)) continue;
                    if (sibObj.VarnameList.Names.IndexOf(field) < 0) continue;
                    TryResolveFieldSymbol(sibPath + field, field, result, diag);
                }
            }
        }

        // Navigates PLC type info along a dotted path (may start with a quoted DB name).
        // Returns the PObject for the struct at that path, or null on failure.
        private PObject NavigateToTypeInfo(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Parse DB name
            string dbNameRaw;
            string fieldPath;
            if (path.StartsWith("\""))
            {
                int closeQ = path.IndexOf('"', 1);
                if (closeQ < 0) return null;
                dbNameRaw = path.Substring(1, closeQ - 1);
                fieldPath = path.Substring(closeQ + 1).TrimStart('.');
            }
            else
            {
                int dot = path.IndexOf('.');
                if (dot < 0) { dbNameRaw = path; fieldPath = ""; }
                else { dbNameRaw = path.Substring(0, dot); fieldPath = path.Substring(dot + 1); }
            }

            List<S7CommPlusConnection.DatablockInfo> dbs;
            if (_conn.GetListOfDatablocks(out dbs) != 0) return null;
            var dbInfo = dbs.Find(d => string.Equals(d.db_name, dbNameRaw, StringComparison.OrdinalIgnoreCase));
            if (dbInfo == null) return null;

            PObject obj = _conn.getTypeInfoByRelId(dbInfo.db_block_ti_relid);
            if (obj?.VarnameList == null) return null;

            if (!string.IsNullOrEmpty(fieldPath))
            {
                foreach (string part in fieldPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int idx = obj.VarnameList?.Names?.IndexOf(part) ?? -1;
                    if (idx < 0) return null;
                    var vte = obj.VartypeList.Elements[idx];
                    if (!vte.OffsetInfoType.HasRelation()) return null;
                    var rel = (IOffsetInfoType_Relation)vte.OffsetInfoType;
                    obj = _conn.getTypeInfoByRelId(rel.GetRelationId());
                    if (obj?.VarnameList == null) return null;
                }
            }
            return obj;
        }

        // Builds VarInfo list for each array element using PLC type-info.
        // Called when _varInfoList is empty (tree-browse path, no full Browse() done).
        // Handles both "DB".Array[n].Field (scalar in struct array) and "DB".Array
        // (array container selected directly — enumerate all elements).
        private List<VarInfo> BuildArrayVarInfosFromSymbol(string pickedSymbol, string arrayPrefix)
        {
            if (_arrayVarInfoCache.TryGetValue(pickedSymbol, out var cached) && cached.Count > 0)
                return cached;

            var result = new List<VarInfo>();
            if (!IsConnected) return result;

            string dbPrefix = ExtractDbPrefix(pickedSymbol);
            if (dbPrefix == null)
            {
                var vi = GetVarInfoBySymbol(pickedSymbol);
                if (vi != null) result.Add(vi);
                CacheArrayResult(pickedSymbol, result);
                return result;
            }

            int bracketOpen = pickedSymbol.LastIndexOf('[');
            string arrayFieldPath;
            string subSuffix;

            if (bracketOpen >= 0)
            {
                int bracketClose = pickedSymbol.IndexOf(']', bracketOpen);
                subSuffix = (bracketClose >= 0 && bracketClose < pickedSymbol.Length - 1)
                    ? pickedSymbol.Substring(bracketClose + 1)
                    : "";
                string basePart = pickedSymbol.Substring(0, bracketOpen);
                arrayFieldPath = basePart.Substring(dbPrefix.Length);
            }
            else
            {
                // Symbol IS the array field (array-container node selected) — enumerate all elements
                arrayFieldPath = pickedSymbol.Substring(dbPrefix.Length);
                subSuffix = "";
            }

            List<S7CommPlusConnection.DatablockInfo> dbs;
            if (_conn.GetListOfDatablocks(out dbs) != 0) return result;

            string dbNameRaw = dbPrefix.TrimEnd('.');
            if (dbNameRaw.StartsWith("\"") && dbNameRaw.EndsWith("\""))
                dbNameRaw = dbNameRaw.Substring(1, dbNameRaw.Length - 2);

            var dbInfo = dbs.Find(d => string.Equals(d.db_name, dbNameRaw, StringComparison.OrdinalIgnoreCase));
            if (dbInfo == null) return result;

            PObject currentObj = _conn.getTypeInfoByRelId(dbInfo.db_block_ti_relid);
            if (currentObj?.VarnameList == null) return result;

            string[] pathParts = arrayFieldPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            PVartypeListElement arrayVarType = null;

            for (int pi = 0; pi < pathParts.Length; pi++)
            {
                string part = pathParts[pi];
                int idx = currentObj.VarnameList?.Names?.IndexOf(part) ?? -1;
                if (idx < 0) return result;
                var vte = currentObj.VartypeList.Elements[idx];

                if (pi == pathParts.Length - 1)
                {
                    arrayVarType = vte;
                    break;
                }
                else if (vte.OffsetInfoType.HasRelation())
                {
                    var rel = (IOffsetInfoType_Relation)vte.OffsetInfoType;
                    currentObj = _conn.getTypeInfoByRelId(rel.GetRelationId());
                    if (currentObj == null) return result;
                }
                else
                {
                    return result;
                }
            }

            if (arrayVarType == null || !arrayVarType.OffsetInfoType.Is1Dim()) return result;

            var dim = (IOffsetInfoType_1Dim)arrayVarType.OffsetInfoType;
            int lowerBound = dim.GetArrayLowerBounds();
            int count = (int)dim.GetArrayElementCount();

            // When elements are structs and no sub-field was specified, enumerate top-level scalar
            // fields and byte/char array fields (shown as decoded strings in the grid).
            List<string> scalarFields = null;
            List<(string name, int lb, int elemCount)> byteArrayFields = null;

            if (string.IsNullOrEmpty(subSuffix) && arrayVarType.OffsetInfoType.HasRelation())
            {
                var rel = (IOffsetInfoType_Relation)arrayVarType.OffsetInfoType;
                PObject elemType = _conn.getTypeInfoByRelId(rel.GetRelationId());
                if (elemType?.VarnameList != null)
                {
                    scalarFields   = new List<string>();
                    byteArrayFields = new List<(string, int, int)>();
                    for (int fi = 0; fi < elemType.VarnameList.Names.Count; fi++)
                    {
                        var fvt = elemType.VartypeList.Elements[fi];
                        if (!fvt.OffsetInfoType.Is1Dim() && !fvt.OffsetInfoType.IsMDim() && !fvt.OffsetInfoType.HasRelation())
                        {
                            scalarFields.Add(elemType.VarnameList.Names[fi]);
                        }
                        else if (fvt.OffsetInfoType.Is1Dim() && !fvt.OffsetInfoType.HasRelation() &&
                                 (fvt.Softdatatype == Softdatatype.S7COMMP_SOFTDATATYPE_BYTE ||
                                  fvt.Softdatatype == Softdatatype.S7COMMP_SOFTDATATYPE_CHAR ||
                                  fvt.Softdatatype == Softdatatype.S7COMMP_SOFTDATATYPE_USINT))
                        {
                            var dim2 = (IOffsetInfoType_1Dim)fvt.OffsetInfoType;
                            byteArrayFields.Add((elemType.VarnameList.Names[fi], dim2.GetArrayLowerBounds(), (int)dim2.GetArrayElementCount()));
                        }
                    }
                }
            }

            for (int i = lowerBound; i < lowerBound + count; i++)
            {
                if (scalarFields != null)
                {
                    foreach (var field in scalarFields)
                    {
                        string sym = $"{dbPrefix}{arrayFieldPath}[{i}].{field}";
                        PlcTag tag = _conn.getPlcTagBySymbol(sym);
                        if (tag == null) continue;
                        var vi = new VarInfo();
                        vi.Name = sym;
                        vi.AccessSequence = tag.Address.GetAccessString();
                        vi.Softdatatype = tag.Datatype;
                        result.Add(vi);
                    }
                    if (byteArrayFields != null)
                    {
                        foreach (var (bfName, bfLb, bfCount) in byteArrayFields)
                        {
                            // Resolve up to 256 bytes; very large arrays (e.g. 500-byte buffers)
                            // are null-terminated so we stop rendering at the first \0 anyway.
                            int limit = Math.Min(bfCount, 256);
                            for (int bi = bfLb; bi < bfLb + limit; bi++)
                            {
                                string sym = $"{dbPrefix}{arrayFieldPath}[{i}].{bfName}[{bi}]";
                                PlcTag tag = _conn.getPlcTagBySymbol(sym);
                                if (tag == null) continue;
                                var vi = new VarInfo();
                                vi.Name = sym;
                                vi.AccessSequence = tag.Address.GetAccessString();
                                vi.Softdatatype = tag.Datatype;
                                result.Add(vi);
                            }
                        }
                    }
                }
                else
                {
                    string sym = $"{dbPrefix}{arrayFieldPath}[{i}]{subSuffix}";
                    PlcTag tag = _conn.getPlcTagBySymbol(sym);
                    if (tag == null) continue;
                    var vi = new VarInfo();
                    vi.Name = sym;
                    vi.AccessSequence = tag.Address.GetAccessString();
                    vi.Softdatatype = tag.Datatype;
                    result.Add(vi);
                }
            }

            CacheArrayResult(pickedSymbol, result);
            return result;
        }

        public void ResetArrayCache(string tagName = null)
        {
            if (tagName == null)
                _arrayVarInfoCache.Clear();
            else
                _arrayVarInfoCache.Remove(tagName);
        }

        private void CacheArrayResult(string tagName, List<VarInfo> list)
        {
            _arrayVarInfoCache[tagName] = list;
        }

        // Returns the path to the struct that CONTAINS the array field (trailing dot included).
        // "DB_Host_Interface".Send.CounterUpdate.Data[0].Location → "DB_Host_Interface".Send.CounterUpdate.
        // "DB_Host_Interface".Send.CounterUpdate.Data              → "DB_Host_Interface".Send.CounterUpdate.
        // "DB_CounterFifo".Buffer[0].Field                        → "DB_CounterFifo".
        // "DB_CounterFifo".Buffer                                  → "DB_CounterFifo".
        private static string ExtractParentPath(string arrayVarName)
        {
            // Strip everything from '[' onwards (or use full name if no bracket)
            int bracketOpen = arrayVarName.LastIndexOf('[');
            string beforeArray = bracketOpen >= 0
                ? arrayVarName.Substring(0, bracketOpen)
                : arrayVarName;

            // searchFrom: skip past the closing quote of the DB name so we don't
            // mistake it for a field separator dot
            int searchFrom = 0;
            if (beforeArray.StartsWith("\""))
            {
                int closeQuote = beforeArray.IndexOf('"', 1);
                searchFrom = closeQuote >= 0 ? closeQuote + 1 : 0;
            }

            int lastDot = beforeArray.LastIndexOf('.');
            if (lastDot <= searchFrom)
                return ExtractDbPrefix(arrayVarName); // array is directly inside the DB

            return beforeArray.Substring(0, lastDot + 1);
        }

        // Extracts the DB-level prefix from a symbolic variable name.
        // "DB_CounterFifo".Buffer[0].Field → "DB_CounterFifo".
        // DB_CounterFifo.Buffer[0].Field   → DB_CounterFifo.
        private static string ExtractDbPrefix(string varName)
        {
            if (string.IsNullOrEmpty(varName)) return null;
            if (varName.StartsWith("\""))
            {
                int closeQuote = varName.IndexOf('"', 1);
                if (closeQuote < 0) return null;
                return varName.Substring(0, closeQuote + 1) + ".";
            }
            int dotIdx = varName.IndexOf('.');
            return dotIdx >= 0 ? varName.Substring(0, dotIdx + 1) : null;
        }

        private static string GetArrayPrefix(string name)
        {
            int idx = name.LastIndexOf('[');
            return idx >= 0 ? name.Substring(0, idx + 1) : name;
        }

        // Returns the prefix up to and including the FIRST '[' that belongs to the outermost
        // array (skipping past any quoted DB name). Using LastIndexOf would incorrectly pick
        // up byte-sub-array brackets like "DB".Array[2].ByteField[0].
        private static string OuterArrayPrefix(string varName)
        {
            int searchFrom = 0;
            if (varName.StartsWith("\""))
            {
                int q = varName.IndexOf('"', 1);
                searchFrom = q >= 0 ? q + 1 : 0;
            }
            int first = varName.IndexOf('[', searchFrom);
            return first >= 0 ? varName.Substring(0, first + 1) : varName;
        }

        private static int ExtractArrayIndex(string name, string prefix)
        {
            if (prefix == name) return 0;
            int prefixLen = prefix.Length;
            if (name.Length <= prefixLen) return 0;
            int close = name.IndexOf(']', prefixLen);
            if (close > prefixLen && int.TryParse(name.Substring(prefixLen, close - prefixLen), out int idx))
                return idx;
            return 0;
        }

        private static bool IsInFifoWindow(int index, int readPtr, int writePtr, int capacity, int stored)
        {
            if (capacity <= 0) return false;

            // Use RecordsStored when available — it's unambiguous regardless of whether
            // writePtr means "next-to-write" or "last-written" on the PLC side.
            if (stored >= 0)
            {
                if (stored == 0) return false;
                if (stored >= capacity) return true;
                int relPos = (index - readPtr + capacity) % capacity;
                return relPos < stored;
            }

            // Fallback: pointer-only comparison when RecordsStored is not configured.
            if (readPtr == writePtr) return false;
            if (writePtr > readPtr) return index >= readPtr && index < writePtr;
            return index >= readPtr || index < writePtr;
        }

        // Merges runs of consecutive BYTE/CHAR/USINT entries that share the same parent field
        // (e.g. "DB".Array[5].Msg[0], [1], ... → one entry "DB".Array[5].Msg = "Hello")
        private static List<FifoEntry> CollapseByteArrays(List<FifoEntry> entries, List<uint> softdatatypes)
        {
            if (entries.Count == 0) return entries;
            var result = new List<FifoEntry>(entries.Count);
            int i = 0;
            while (i < entries.Count)
            {
                uint sdt = softdatatypes[i];
                bool isByteType = sdt == Softdatatype.S7COMMP_SOFTDATATYPE_BYTE ||
                                  sdt == Softdatatype.S7COMMP_SOFTDATATYPE_CHAR ||
                                  sdt == Softdatatype.S7COMMP_SOFTDATATYPE_USINT;

                if (!isByteType) { result.Add(entries[i++]); continue; }

                // Only treat as a byte-array sub-element when there's a second [...] after the first ]
                string varName = entries[i].Variable;
                int firstClose = varName.IndexOf(']');
                bool isSubIndex = firstClose >= 0 && varName.IndexOf('[', firstClose + 1) >= 0;

                if (!isSubIndex) { result.Add(entries[i++]); continue; }

                // Prefix up to and including the final '[' (e.g. "DB".Array[5].Msg[)
                int lastOpen = varName.LastIndexOf('[');
                string bytePrefix = varName.Substring(0, lastOpen + 1);

                // Drain ALL consecutive entries with this prefix into a local list first,
                // so that j always advances past the whole byte field even when the first
                // byte is a null terminator (otherwise each 0x00 byte becomes its own entry).
                int j = i;
                var byteValues = new List<(string val, uint dt)>();
                while (j < entries.Count &&
                       entries[j].Variable.StartsWith(bytePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    byteValues.Add((entries[j].Value, softdatatypes[j]));
                    j++;
                }

                // Build the display string, stopping at the first null terminator.
                var sb = new System.Text.StringBuilder();
                foreach (var (val, dt) in byteValues)
                {
                    string piece = ByteValueToDisplay(val, dt);
                    if (piece == "\0") break;
                    sb.Append(piece);
                }
                string str = sb.ToString();

                result.Add(new FifoEntry
                {
                    ArrayIndex = entries[i].ArrayIndex,
                    Variable   = bytePrefix.TrimEnd('['),
                    Value      = str,
                });
                i = j;
            }
            return result;
        }

        private static string ByteValueToDisplay(string tagValue, uint softdatatype)
        {
            if (tagValue == "(no data)") return "[??]";
            byte b;
            if (softdatatype == Softdatatype.S7COMMP_SOFTDATATYPE_CHAR)
                b = tagValue.Length > 0 ? (byte)tagValue[0] : (byte)0;
            else if (!byte.TryParse(tagValue, out b))
                return "[??]";
            if (b == 0) return "\0"; // null terminator sentinel — caller stops here
            if (b >= 0x20 && b <= 0x7E) return ((char)b).ToString(); // printable ASCII
            return $"${b:X2}"; // non-printable: show hex code
        }

        // Driver's tag.ToString() returns "HH: value" (e.g. "C0: 1") — strip the prefix.
        private static string TagValueString(PlcTag tag)
        {
            string s = tag.ToString();
            int colon = s.IndexOf(": ");
            return colon >= 0 ? s.Substring(colon + 2).Trim() : s.Trim();
        }

        private static int TagToInt(PlcTag tag)
        {
            if ((tag.Quality & PlcTagQC.TAG_QUALITY_MASK) != PlcTagQC.TAG_QUALITY_GOOD)
                return -1;
            string s = TagValueString(tag);
            if (int.TryParse(s, out int v))
                return v;
            if (uint.TryParse(s, out uint u))
                return (int)u;
            return -1;
        }

    }
}
