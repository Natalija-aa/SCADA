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

        // pozadinske niti za skeniranje mogu jos malo raditi i posle StopScan()
        // (StopScan samo postavi flag, ne ceka da se nit zaista ugasi), pa moze
        // da stigne poziv CheckAlarms bas kad se glavni prozor zatvara i ContextClass.Instance
        // se vec dispose-uje - ovaj flag i try/catch ispod to sprecavaju da obore aplikaciju
        private volatile bool isShuttingDown;

        public void Shutdown() => isShuttingDown = true;

        public void Initialize(Action<Action> uiDispatcher)
        {
            dispatcher = uiDispatcher;
            var context = ContextClass.Instance;

            foreach (var ai in context.Tags.OfType<AnalogInput>().ToList())
            {
                ai.ValueChanged -= OnAIValueChanged;
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
            // -= pa += da se izbjegne dupla pretplata ako se StartTag pozove vise puta
            // nad istim tagom (npr. Scan OFF pa ponovo Scan ON)
            ai.ValueChanged -= OnAIValueChanged;
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
            if (isShuttingDown) return;

            try
            {
                CheckAlarmsCore(tagName, value);
            }
            catch (InvalidOperationException)
            {
                // aplikacija se u medjuvremenu zatvorila i kontekst je dispose-ovan - bezopasno preskoci
            }
        }

        private void CheckAlarmsCore(string tagName, double value)
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
