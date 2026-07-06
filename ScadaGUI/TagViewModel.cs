using System.ComponentModel;
using DataConcentrator;
using DataConcentrator.Model;

namespace ScadaGUI
{
    public enum AlarmStatus { None, Active, Acknowledged }

    public class TagViewModel : INotifyPropertyChanged
    {
        public Tag Tag { get; }

        public string Name => Tag.Name;
        public string Description => Tag.Description;
        public string TagType { get; }
        public string IOAddress { get; }

        private double currentValue;
        public double CurrentValue
        {
            get => currentValue;
            set { currentValue = value; OnPropertyChanged(nameof(CurrentValue)); }
        }

        private AlarmStatus alarmStatus = AlarmStatus.None;
        public AlarmStatus AlarmStatus
        {
            get => alarmStatus;
            set { alarmStatus = value; OnPropertyChanged(nameof(AlarmStatus)); }
        }

        public bool IsAI => Tag is AnalogInput;
        public bool IsInput => Tag is AnalogInput || Tag is DigitalInput;
        public bool IsOutput => Tag is AnalogOutput || Tag is DigitalOutput;

        public string ScanBtnText
        {
            get
            {
                if (Tag is AnalogInput ai) return ai.IsScanning ? "Scan OFF" : "Scan ON";
                if (Tag is DigitalInput di) return di.IsScanning ? "Scan OFF" : "Scan ON";
                return "";
            }
        }

        // IsScanning se menja iz pozadinske niti (StartScan/StopScan), pa se dugme
        // Scan ON/OFF ne osvjezava samo od sebe - ovo se zove iz refresh tajmera
        public void RefreshScanState() => OnPropertyChanged(nameof(ScanBtnText));

        public TagViewModel(Tag tag)
        {
            Tag = tag;
            if (tag is AnalogInput) { TagType = "AI"; IOAddress = ((AnalogInput)tag).IOAddress; }
            else if (tag is AnalogOutput) { TagType = "AO"; IOAddress = ((AnalogOutput)tag).IOAddress; }
            else if (tag is DigitalInput) { TagType = "DI"; IOAddress = ((DigitalInput)tag).IOAddress; }
            else if (tag is DigitalOutput) { TagType = "DO"; IOAddress = ((DigitalOutput)tag).IOAddress; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
