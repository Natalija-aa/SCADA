using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using DataConcentrator;
using DataConcentrator.Model;

namespace ScadaGUI
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<TagViewModel> tagList = new ObservableCollection<TagViewModel>();
        private DispatcherTimer refreshTimer;

        public MainWindow()
        {
            InitializeComponent();
            dgTags.ItemsSource = tagList;

            DataConcentratorManager.Instance.AlarmActivated += OnAlarmActivated;
            DataConcentratorManager.Instance.Initialize(action => Dispatcher.Invoke(action));

            LoadTags();

            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            refreshTimer.Tick += (s, e) => RefreshValues();
            refreshTimer.Start();
        }

        private void LoadTags()
        {
            tagList.Clear();
            foreach (var tag in ContextClass.Instance.Tags.ToList())
                tagList.Add(new TagViewModel(tag));
        }

        private void RefreshValues()
        {
            foreach (var vm in tagList)
            {
                if (vm.Tag is AnalogInput ai) vm.CurrentValue = ai.CurrentValue;
                else if (vm.Tag is DigitalInput di) vm.CurrentValue = di.CurrentValue;
                else if (vm.Tag is AnalogOutput ao) vm.CurrentValue = ao.InitialValue;
                else if (vm.Tag is DigitalOutput dout) vm.CurrentValue = dout.InitialValue;

                if (vm.IsInput) vm.RefreshScanState();
            }

            using (var ctx = ContextClass.CreateNew())
            {
                foreach (var vm in tagList.Where(v => v.IsAI))
                {
                    var alarms = ctx.Alarms.Where(a => a.TagName == vm.Name).ToList();
                    if (alarms.Any(a => a.State == AlarmState.Active))
                        vm.AlarmStatus = AlarmStatus.Active;
                    else if (alarms.Any(a => a.State == AlarmState.Acknowledged))
                        vm.AlarmStatus = AlarmStatus.Acknowledged;
                    else
                        vm.AlarmStatus = AlarmStatus.None;
                }
            }
        }

        private void OnAlarmActivated(int activatedAlarmId)
        {
            using (var ctx = ContextClass.CreateNew())
            {
                var aa = ctx.ActivatedAlarms.Find(activatedAlarmId);
                if (aa != null)
                    lblStatus.Text = $"[{aa.TimeStamp:HH:mm:ss}] ALARM: {aa.Message} (tag: {aa.TagName})";
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddWindow { Owner = this };
            win.ShowDialog();
            if (win.TagAdded) LoadTags();
        }

        private void BtnDeleteTag_Click(object sender, RoutedEventArgs e)
        {
            var vm = dgTags.SelectedItem as TagViewModel;
            if (vm == null) { MessageBox.Show("Odaberite tag za brisanje."); return; }

            if (MessageBox.Show($"Obrisati tag '{vm.Name}'?", "Potvrda", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            if (vm.Tag is AnalogInput ai) ai.StopScan();
            if (vm.Tag is DigitalInput di) di.StopScan();

            ContextClass.Instance.Tags.Remove(vm.Tag);
            ContextClass.Instance.SaveChanges();
            Logger.Log(Logger.LogType.TagCUD, $"Obrisan tag {vm.Name}");
            LoadTags();
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as FrameworkElement)?.Tag as TagViewModel;
            if (vm == null) return;
            new AlarmDetailsWindow(vm.Name) { Owner = this }.ShowDialog();
        }

        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as FrameworkElement)?.Tag as TagViewModel;
            if (vm == null) return;
            new WriteValueWindow(vm.Tag) { Owner = this }.ShowDialog();
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var vm = (sender as FrameworkElement)?.Tag as TagViewModel;
            if (vm == null) return;

            if (vm.Tag is AnalogInput ai)
            {
                if (ai.IsScanning) ai.StopScan(); else { DataConcentratorManager.Instance.StartTag(ai); }
                ContextClass.Instance.SaveChanges();
            }
            else if (vm.Tag is DigitalInput di)
            {
                if (di.IsScanning) di.StopScan(); else { DataConcentratorManager.Instance.StartTag(di); }
                ContextClass.Instance.SaveChanges();
            }
            Logger.Log(Logger.LogType.TagUpdate, $"Scan toggle za tag {vm.Name}");
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "Text fajlovi (*.txt)|*.txt", FileName = "report" };
            if (dlg.ShowDialog() != true) return;

            var history = DataConcentratorManager.Instance.GetReportHistory();
            var sb = new StringBuilder();
            sb.AppendLine("SCADA Report - vrijednosti analognih ulaza u zoni (high+low)/2 ± 5");
            sb.AppendLine(new string('-', 60));
            foreach (var (tagName, time, value) in history)
                sb.AppendLine($"{time:yyyy-MM-dd HH:mm:ss} | {tagName,-20} | {value:F3}");

            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show($"Report generisan: {dlg.FileName}");
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "scada_config", DefaultExt = ".json" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                ConfigurationService.ExportToJson(dlg.FileName);
                MessageBox.Show("Konfiguracija eksportovana.");
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.Error, $"Export greška: {ex.Message}");
                MessageBox.Show("Greška: " + ex.Message);
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON (*.json)|*.json", DefaultExt = ".json" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var newInputs = ConfigurationService.ImportFromJson(dlg.FileName);
                foreach (var tag in newInputs)
                {
                    if (tag is AnalogInput ai && ai.IsScanning)
                        DataConcentratorManager.Instance.StartTag(ai);
                    else if (tag is DigitalInput di && di.IsScanning)
                        DataConcentratorManager.Instance.StartTag(di);
                }
                LoadTags();
                MessageBox.Show("Konfiguracija importovana.");
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.Error, $"Import greška: {ex.Message}");
                MessageBox.Show("Greška: " + ex.Message);
            }
        }

        private void BtnTraceSettings_Click(object sender, RoutedEventArgs e)
        {
            new TraceSettingsWindow { Owner = this }.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log(Logger.LogType.Login, "Korisnik se odjavio (zatvaranje aplikacije).");
            refreshTimer?.Stop();
            DataConcentratorManager.Instance.Shutdown();
            foreach (var ai in ContextClass.Instance.Tags.OfType<AnalogInput>()) ai.StopScan();
            foreach (var di in ContextClass.Instance.Tags.OfType<DigitalInput>()) di.StopScan();
            PLC.StopSimulator();
            ContextClass.Instance.SaveChanges();
            ContextClass.Instance.Dispose();
        }
    }
}
