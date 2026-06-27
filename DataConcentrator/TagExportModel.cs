using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataConcentrator
{
    [DataContract]
    public class TagExportModel
    {
        [DataMember] public string Type        { get; set; }
        [DataMember] public string Name        { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public string IOAddress   { get; set; }
        [DataMember] public int    ScanTime    { get; set; }
        [DataMember] public bool   IsScanning  { get; set; }
        [DataMember] public double LowLimit    { get; set; }
        [DataMember] public double HighLimit   { get; set; }
        [DataMember] public string Units       { get; set; }
        [DataMember] public double Deadband    { get; set; }
        [DataMember] public double Hysteresis  { get; set; }
        [DataMember] public double InitialValue { get; set; }
    }

    [DataContract]
    public class ConfigurationExport
    {
        [DataMember] public List<TagExportModel> Tags { get; set; }
    }
}
