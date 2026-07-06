using System;
using System.ComponentModel.DataAnnotations;

namespace DataConcentrator.Model
{
    public class ActivatedAlarm
    {
        [Key]
        public int Id { get; set; } // za istorijski zapis

        // namjerno bez navigacije/FK ka Alarm-u: ovo je trajan istorijski zapis
        // i ne smije nestati (cascade delete) ako se sam alarm kasnije obrise
        public int AlarmId { get; set; }    // iz alarm klase - koji alarm je okinut
        public string TagName { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; } // kada se tacno desilo
    }
}
