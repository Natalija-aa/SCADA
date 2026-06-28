using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;

namespace DataConcentrator.Model
{
    public class DigitalInput : Tag
    {
        public string IOAddress { get; set; }
        public int ScanTime { get; set; }
        public bool IsScanning { get; set; }

        [NotMapped] public double CurrentValue { get; set; }
        [NotMapped] private Thread scanThread;
        [NotMapped] private bool isRunning;

        public event Action<string, double> ValueChanged;

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
            while (isRunning)
            {
                Thread.Sleep(ScanTime > 0 ? ScanTime : 1000);
                double newValue = PLC.Instance.GetValue(IOAddress);
                CurrentValue = newValue;
                ValueChanged?.Invoke(Name, newValue);
            }
        }
    }
}
