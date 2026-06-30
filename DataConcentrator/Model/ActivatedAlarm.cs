using System;
using System.ComponentModel.DataAnnotations;

namespace DataConcentrator.Model
{
    public class ActivatedAlarm
    {
        [Key]
        public int Id { get; set; }
        public int AlarmId { get; set; }    // iz alarm klase
        public string TagName { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; } // kada se tacno desilo

        public virtual Alarm Alarm { get; set; }    // ceo objekat
    }
}
