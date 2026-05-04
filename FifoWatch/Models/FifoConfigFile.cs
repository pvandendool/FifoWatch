using System.Collections.Generic;
using System.Xml.Serialization;

namespace FifoWatch.Models
{
    [XmlRoot("FifoWatchConfig")]
    public class FifoConfigFile
    {
        public int PollIntervalMs { get; set; } = 500;

        [XmlArray("Monitors")]
        [XmlArrayItem("Monitor")]
        public List<FifoMonitorConfig> Monitors { get; set; } = new List<FifoMonitorConfig>();
    }

    public class FifoMonitorConfig
    {
        public string       Name          { get; set; }
        public FifoTagConfig ArrayTag     { get; set; }
        public FifoTagConfig HeadTag      { get; set; }
        public FifoTagConfig TailTag      { get; set; }
        public FifoTagConfig CountTag     { get; set; }
        public FifoTagConfig MaxRecordsTag { get; set; }
    }

    public class FifoTagConfig
    {
        public string Name           { get; set; }
        public string AccessSequence { get; set; }
        public uint   Softdatatype   { get; set; }
    }
}
