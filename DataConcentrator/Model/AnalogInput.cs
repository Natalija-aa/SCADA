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
        [NotMapped] private Thread scanThread;
        [NotMapped] private bool isRunning;

        public event Action<string, double> ValueChanged;

        public AnalogInput() { Alarms = new List<Alarm>(); }

        public void StartScan()
        {
            if (isRunning) return;
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
                Thread.Sleep(ScanTime > 0 ? ScanTime : 1000);
                double newValue = PLC.Instance.GetAnalogValue(IOAddress);
                if (prevValue == double.MinValue || Math.Abs(newValue - prevValue) >= Deadband)
                {
                    prevValue = newValue;
                    CurrentValue = newValue;
                    ValueChanged?.Invoke(Name, newValue);
                }
            }
        }
    }
}
