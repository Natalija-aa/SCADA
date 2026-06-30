using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator.Model
{
    public enum AlarmState { Inactive, Active, Acknowledged }

    public class Alarm
    {
        [Key]
        public int Id { get; set; } // primarni kljuc
        public string TagName { get; set; } // kao most ka analog input
        public double LimitValue { get; set; }  // granicna vrednost koja okida alarm
        public bool TriggerAbove { get; set; }
        // true - prelazi granicu
        // false - ispod granice
        public string Message { get; set; } // poruka za operatora
        public AlarmState State { get; set; }

        [ForeignKey("TagName")] // veza izmedju 2 tabele - AnalogInput.cs
        public virtual AnalogInput Tag { get; set; }
    }
}
