using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataConcentrator
{
    [DataContract]  // serijalizacija - pretvara objekat iz memorije
                    // u niz podataka koji se mogu spremiti ili poslati
    public class TagExportModel
    {
        [DataMember] public string Type        { get; set; }
        [DataMember] public string Name        { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public string IOAddress   { get; set; }
        [DataMember] public int    ScanTime    { get; set; }    // AI i DI
        [DataMember] public bool   IsScanning  { get; set; }    // AI i DI
        [DataMember] public double LowLimit    { get; set; }    // AI i AO
        [DataMember] public double HighLimit   { get; set; }    // AI i AO
        [DataMember] public string Units       { get; set; }    // AI i AO
        [DataMember] public double Deadband    { get; set; }    // AI
        [DataMember] public double Hysteresis  { get; set; }    // AI
        [DataMember] public double InitialValue { get; set; }   // AO i DO
    }

    [DataContract]
    public class ConfigurationExport
    {
        [DataMember] public List<TagExportModel> Tags { get; set; }
    }
}
