using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator.Model
{
    public enum AlarmState { Inactive, Active, Acknowledged }

    public class Alarm
    {
        [Key]
        public int Id { get; set; }
        public string TagName { get; set; }
        public double LimitValue { get; set; }
        public bool TriggerAbove { get; set; }
        public string Message { get; set; }
        public AlarmState State { get; set; }

        [ForeignKey("TagName")]
        public virtual AnalogInput Tag { get; set; }
    }
}
