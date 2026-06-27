using System;
using System.Collections.Generic;
using System.Linq;
using DataConcentrator.Model;

namespace DataConcentrator
{
    public class DataConcentratorManager
    {
        private static DataConcentratorManager instance;
        public static DataConcentratorManager Instance => instance ?? (instance = new DataConcentratorManager());

        public event Action<int> AlarmActivated;

        private readonly List<(string TagName, DateTime Time, double Value)> reportHistory
            = new List<(string, DateTime, double)>();

        private readonly object historyLock = new object();
        private Action<Action> dispatcher;

        public void Initialize(Action<Action> uiDispatcher)
        {
            dispatcher = uiDispatcher;
            var context = ContextClass.Instance;

            foreach (var ai in context.Tags.OfType<AnalogInput>().ToList())
            {
                ai.ValueChanged += OnAIValueChanged;
                if (ai.IsScanning) ai.StartScan();
            }

            foreach (var di in context.Tags.OfType<DigitalInput>().ToList())
            {
                if (di.IsScanning) di.StartScan();
            }
        }

        public void StartTag(AnalogInput ai)
        {
            ai.ValueChanged += OnAIValueChanged;
            ai.StartScan();
        }

        public void StartTag(DigitalInput di) => di.StartScan();

        private void OnAIValueChanged(string tagName, double value)
        {
            dispatcher?.Invoke(() => CheckAlarms(tagName, value));
        }

        private void CheckAlarms(string tagName, double value)
        {
            using (var ctx = ContextClass.CreateNew())
            {
                var tag = ctx.Tags.OfType<AnalogInput>().FirstOrDefault(t => t.Name == tagName);
                double hysteresis = tag?.Hysteresis ?? 0;

                var alarms = ctx.Alarms.Where(a => a.TagName == tagName).ToList();
                foreach (var alarm in alarms)
                {
                    bool conditionMet = alarm.TriggerAbove
                        ? value > alarm.LimitValue
                        : value < alarm.LimitValue;

                    bool conditionCleared = alarm.TriggerAbove
                        ? value < (alarm.LimitValue - hysteresis)
                        : value > (alarm.LimitValue + hysteresis);

                    if (conditionMet && alarm.State == AlarmState.Inactive)
                    {
                        alarm.State = AlarmState.Active;
                        var activated = new ActivatedAlarm
                        {
                            AlarmId = alarm.Id,
                            TagName = tagName,
                            Message = alarm.Message,
                            TimeStamp = DateTime.Now
                        };
                        ctx.ActivatedAlarms.Add(activated);
                        ctx.SaveChanges();
                        Logger.Log(Logger.LogType.AlarmCUD, $"Alarm aktiviran: {alarm.Message} na tagu {tagName}");
                        AlarmActivated?.Invoke(activated.Id);
                    }
                    else if (conditionCleared && alarm.State == AlarmState.Acknowledged)
                    {
                        // samo acknowledged alarmi mogu postati Inactive automatski
                        alarm.State = AlarmState.Inactive;
                        ctx.SaveChanges();
                    }
                }
            }

            var ai = ContextClass.Instance.Tags.OfType<AnalogInput>().FirstOrDefault(t => t.Name == tagName);
            if (ai != null)
            {
                double mid = (ai.HighLimit + ai.LowLimit) / 2.0;
                if (Math.Abs(value - mid) <= 5.0)
                {
                    lock (historyLock)
                        reportHistory.Add((tagName, DateTime.Now, value));
                }
            }
        }

        public IReadOnlyList<(string TagName, DateTime Time, double Value)> GetReportHistory()
        {
            lock (historyLock)
                return reportHistory.AsReadOnly();
        }
    }
}
