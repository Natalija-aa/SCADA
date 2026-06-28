using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;

namespace DataConcentrator.Model
{
    public class AnalogInput : Tag
    {
        public string IOAddress { get; set; }
        public int ScanTime { get; set; }
        public bool IsScanning { get; set; }
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; }
        public double Deadband { get; set; }
        public double Hysteresis { get; set; }

        public virtual ICollection<Alarm> Alarms { get; set; }

        [NotMapped] public double CurrentValue { get; set; }
        [NotMapped] private Thread scanThread;  // cita vrednosti bez blokiranja ostatka pprograma
        [NotMapped] private bool isRunning;

        public event Action<string, double> ValueChanged;   // vrednoist se promenila vise od deadband-a

        public AnalogInput() { Alarms = new List<Alarm>(); }    // inicijalizuje listu alarma

        public void StartScan()
        {
            if (isRunning) return;  //ako redi vec, ne radi nista
            isRunning = true;
            IsScanning = true;
            scanThread = new Thread(ScanLoop) { IsBackground = true, Name = "Scan_" + Name };
            scanThread.Start();
        }

        public void StopScan()
        {
            isRunning = false;
            IsScanning = false;
        }

        private void ScanLoop()
        {
            double prevValue = double.MinValue;
            while (isRunning)
            {
                Thread.Sleep(ScanTime > 0 ? ScanTime : 1000);    // da ne bi procesor radio na 100% snage 
                // ako je 0 spava 1s, inace za ScaningTime
                double newValue = PLC.Instance.GetValue(IOAddress); // vrijednost sa zadate adrese
                // da li je ovo prvo citanje ili da li je razlika veca od deadbanda
                if (prevValue == double.MinValue || Math.Abs(newValue - prevValue) >= Deadband)
                {
                    prevValue = newValue;
                    CurrentValue = newValue;
                    ValueChanged?.Invoke(Name, newValue);   // javi da se promenila vrednst
                }
            }
        }
    }
}
