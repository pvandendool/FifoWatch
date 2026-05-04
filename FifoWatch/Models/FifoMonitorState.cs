using System;
using System.Collections.Generic;

namespace FifoWatch.Models
{
    public class FifoMonitorState
    {
        public string         Name       { get; set; } = "FIFO";
        public FifoDefinition Definition { get; set; } = new FifoDefinition();

        public bool            IsPolling      { get; set; }
        public List<FifoEntry> LastEntries    { get; set; }
        public bool            DisplayIsStale { get; set; }
        public int             LastHead       { get; set; } = -1;
        public int             LastTail       { get; set; } = -1;
        public int             LastCount      { get; set; } = -1;
        public int             LastMaxRec     { get; set; } = -1;
        public string          LastError      { get; set; }
        public DateTime        NextPollDue    { get; set; } = DateTime.MinValue;
    }
}
