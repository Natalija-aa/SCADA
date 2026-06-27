namespace DataConcentrator.Model
{
    public class AnalogOutput : Tag
    {
        public string IOAddress { get; set; }
        public double InitialValue { get; set; }
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; }
    }
}
