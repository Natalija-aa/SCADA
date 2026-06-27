using System.Windows;
using DataConcentrator;

namespace ScadaGUI
{
    public partial class TraceSettingsWindow : Window
    {
        public TraceSettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            int tw = Logger.TraceWord;
            chkTagCUD.IsChecked      = (tw & (int)Logger.LogType.TagCUD)           != 0;
            chkTagUpdate.IsChecked   = (tw & (int)Logger.LogType.TagUpdate)         != 0;
            chkAlarmCUD.IsChecked    = (tw & (int)Logger.LogType.AlarmCUD)          != 0;
            chkAlarmAck.IsChecked    = (tw & (int)Logger.LogType.AlarmAcknowledge)  != 0;
            chkImportExport.IsChecked= (tw & (int)Logger.LogType.ImportExport)      != 0;
            chkLogin.IsChecked       = (tw & (int)Logger.LogType.Login)             != 0;
            chkError.IsChecked       = (tw & (int)Logger.LogType.Error)             != 0;
            UpdateTraceWordDisplay(tw);
        }

        private void UpdateTraceWordDisplay(int tw)
        {
            txtTraceWord.Text = $"TraceWord (numerički): {tw}";
        }

        private int BuildTraceWord()
        {
            int tw = 0;
            if (chkTagCUD.IsChecked       == true) tw |= (int)Logger.LogType.TagCUD;
            if (chkTagUpdate.IsChecked     == true) tw |= (int)Logger.LogType.TagUpdate;
            if (chkAlarmCUD.IsChecked      == true) tw |= (int)Logger.LogType.AlarmCUD;
            if (chkAlarmAck.IsChecked      == true) tw |= (int)Logger.LogType.AlarmAcknowledge;
            if (chkImportExport.IsChecked  == true) tw |= (int)Logger.LogType.ImportExport;
            if (chkLogin.IsChecked         == true) tw |= (int)Logger.LogType.Login;
            if (chkError.IsChecked         == true) tw |= (int)Logger.LogType.Error;
            return tw;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int tw = BuildTraceWord();
            Logger.TraceWord = tw;
            MessageBox.Show($"Podešavanja sačuvana.\nTraceWord = {tw}",
                "Sačuvano", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
