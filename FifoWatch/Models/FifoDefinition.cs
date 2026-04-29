using S7CommPlusDriver;

namespace FifoWatch.Models
{
    public class FifoDefinition
    {
        public VarInfo ArrayTag         { get; set; }
        public VarInfo HeadTag          { get; set; }  // NextIndexToRead
        public VarInfo TailTag          { get; set; }  // NextIndexToWrite
        public VarInfo CountTag         { get; set; }  // RecordsStored
        public VarInfo MaxRecordsTag    { get; set; }  // MaxNrOfRecords

        public bool IsValid => ArrayTag != null;
    }
}
