using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;

namespace DataConcentrator.Model
{
    public class AnalogInput : Tag
    {
        public int ScanTime { get; set; }
        public bool IsScanning { get; set; }
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; }
        public double Deadband { get; set; }
        public double Hysteresis { get; set; }

        // virtual - lazy loading, ucitava kada trebaju alarmi
        public virtual ICollection<Alarm> Alarms { get; set; }

        // ne cuvaj u bazi
        [NotMapped] public double CurrentValue { get; set; }
        [NotMapped] private Thread scanThread;  // cita vrednosti bez blokiranja ostatka programa
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
            double prevValue = double.MinValue; // kao marker da nije procitana vrednost
            while (isRunning)
            {
                Thread.Sleep(ScanTime > 0 ? ScanTime : 1000);    // ako ScanTime < 0 - 1000ms da se ne optereti CPU
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
