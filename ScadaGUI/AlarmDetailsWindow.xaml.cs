using System.Linq;
using System.Windows;
using DataConcentrator;
using DataConcentrator.Model;

namespace ScadaGUI
{
    public partial class AlarmDetailsWindow : Window
    {
        private readonly string tagName;

        public AlarmDetailsWindow(string tagName)
        {
            InitializeComponent();
            this.tagName = tagName;
            lblTag.Text = $"Alarmi za tag: {tagName}";
            Refresh();
        }

        private void Refresh()
        {
            using (var ctx = ContextClass.CreateNew())
            {
                var alarms = ctx.Alarms
                    .Where(a => a.TagName == tagName)
                    .ToList()
                    .Select(a => new
                    {
                        a.Id, a.LimitValue,
                        Direction = a.TriggerAbove ? "Iznad granice" : "Ispod granice",
                        a.Message,
                        State = a.State.ToString()
                    }).ToList();
                dgAlarms.ItemsSource = alarms;
            }
        }

        private void BtnAck_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgAlarms.SelectedItem;
            if (selected == null) { MessageBox.Show("Odaberite alarm."); return; }

            int id = selected.Id;
            using (var ctx = ContextClass.CreateNew())
            {
                var alarm = ctx.Alarms.Find(id);
                if (alarm != null && alarm.State == AlarmState.Active)
                {
                    alarm.State = AlarmState.Acknowledged;
                    ctx.SaveChanges();
                    Logger.Log(Logger.LogType.AlarmAcknowledge, $"Alarm {id} acknowledged na tagu {tagName}");
                }
            }
            Refresh();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = dgAlarms.SelectedItem;
            if (selected == null) { MessageBox.Show("Odaberite alarm."); return; }

            int id = selected.Id;
            using (var ctx = ContextClass.CreateNew())
            {
                var alarm = ctx.Alarms.Find(id);
                if (alarm != null)
                {
                    ctx.Alarms.Remove(alarm);
                    ctx.SaveChanges();
                    Logger.Log(Logger.LogType.AlarmCUD, $"Obrisan alarm {id} sa taga {tagName}");
                }
            }
            Refresh();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
