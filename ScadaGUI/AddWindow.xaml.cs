using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DataConcentrator;
using DataConcentrator.Model;

namespace ScadaGUI
{
    public partial class AddWindow : Window
    {
        private TextBox txtName, txtDesc, txtAddr, txtScanTime, txtLowLimit,
                        txtHighLimit, txtUnits, txtDeadband, txtHysteresis,
                        txtInitVal, txtAlarmMsg, txtAlarmLimit;
        private CheckBox chkScanning;
        private ComboBox cmbAlarmTag, cmbAlarmDir;

        public bool TagAdded { get; private set; }

        public AddWindow() { InitializeComponent(); BuildFields("AI"); }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pnlFields == null) return;
            string type = ((ComboBoxItem)cmbType.SelectedItem)?.Content?.ToString();
            BuildFields(type);
        }

        private TextBox AddRow(string label, string defaultVal = "")
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            sp.Children.Add(new TextBlock { Text = label, Width = 130, VerticalAlignment = VerticalAlignment.Center });
            var tb = new TextBox { Width = 200, Text = defaultVal };
            sp.Children.Add(tb);
            pnlFields.Children.Add(sp);
            return tb;
        }

        private CheckBox AddCheckRow(string label, bool isChecked = true)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            sp.Children.Add(new TextBlock { Text = label, Width = 130, VerticalAlignment = VerticalAlignment.Center });
            var cb = new CheckBox { IsChecked = isChecked, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(cb);
            pnlFields.Children.Add(sp);
            return cb;
        }

        private ComboBox AddComboRow(string label, System.Collections.Generic.IEnumerable<string> items)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            sp.Children.Add(new TextBlock { Text = label, Width = 130, VerticalAlignment = VerticalAlignment.Center });
            var cb = new ComboBox { Width = 200 };
            foreach (var item in items) cb.Items.Add(item);
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
            sp.Children.Add(cb);
            pnlFields.Children.Add(sp);
            return cb;
        }

        private void BuildFields(string type)
        {
            pnlFields.Children.Clear();
            txtName = txtDesc = txtAddr = txtScanTime = txtLowLimit =
            txtHighLimit = txtUnits = txtDeadband = txtHysteresis =
            txtInitVal = txtAlarmMsg = txtAlarmLimit = null;
            chkScanning = null; cmbAlarmTag = null; cmbAlarmDir = null;

            if (type == "Alarm")
            {
                var aiNames = ContextClass.Instance.Tags.OfType<AnalogInput>().Select(t => t.Name).ToList();
                cmbAlarmTag = AddComboRow("Analog Input tag:", aiNames);
                txtAlarmLimit = AddRow("Granica:", "50");
                cmbAlarmDir = AddComboRow("Aktivira se:", new[] { "Iznad granice", "Ispod granice" });
                txtAlarmMsg = AddRow("Poruka:", "Alarm!");
                return;
            }

            txtName = AddRow("Ime (ID):");
            txtDesc = AddRow("Opis:");
            txtAddr = AddRow("I/O adresa:");

            if (type == "AI" || type == "DI")
            {
                txtScanTime = AddRow("Scan time (ms):", "1000");
                chkScanning = AddCheckRow("Scanning uključen:", true);
            }
            if (type == "AI" || type == "AO")
            {
                txtLowLimit = AddRow("Low limit:", "0");
                txtHighLimit = AddRow("High limit:", "100");
                txtUnits = AddRow("Jedinice:");
                if (type == "AI")
                {
                    txtDeadband = AddRow("Deadband:", "1");
                    txtHysteresis = AddRow("Hysteresis:", "0.5");
                }
            }
            if (type == "AO" || type == "DO")
                txtInitVal = AddRow("Initial value:", "0");
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string type = ((ComboBoxItem)cmbType.SelectedItem)?.Content?.ToString();

            try
            {
                var ctx = ContextClass.Instance;

                if (type == "Alarm")
                {
                    if (cmbAlarmTag.SelectedItem == null) { MessageBox.Show("Odaberite AI tag."); return; }
                    string tagName = cmbAlarmTag.SelectedItem.ToString();
                    ctx.Alarms.Add(new Alarm
                    {
                        TagName = tagName,
                        LimitValue = double.Parse(txtAlarmLimit.Text),
                        TriggerAbove = cmbAlarmDir.SelectedIndex == 0,
                        Message = txtAlarmMsg.Text,
                        State = AlarmState.Inactive
                    });
                    ctx.SaveChanges();
                    Logger.Log(Logger.LogType.AlarmCUD, $"Dodat alarm na tag {tagName}");
                    TagAdded = true;
                    Close();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Unesite ime taga."); return; }
                if (ctx.Tags.Find(txtName.Text) != null) { MessageBox.Show("Tag sa tim imenom već postoji."); return; }

                Tag tag = null;
                switch (type)
                {
                    case "AI":
                    {
                        int scanTime = int.Parse(txtScanTime.Text);
                        if (scanTime <= 0) { MessageBox.Show("Scan time mora biti veći od 0."); return; }
                        double low = double.Parse(txtLowLimit.Text);
                        double high = double.Parse(txtHighLimit.Text);
                        if (high <= low) { MessageBox.Show("High limit mora biti veći od Low limit."); return; }
                        double deadband = double.Parse(txtDeadband.Text);
                        if (deadband < 0) { MessageBox.Show("Deadband ne može biti negativan."); return; }
                        double hysteresis = double.Parse(txtHysteresis.Text);
                        if (hysteresis < 0) { MessageBox.Show("Hysteresis ne može biti negativan."); return; }
                        tag = new AnalogInput
                        {
                            Name = txtName.Text, Description = txtDesc.Text,
                            IOAddress = txtAddr.Text,
                            ScanTime = scanTime,
                            IsScanning = chkScanning.IsChecked == true,
                            LowLimit = low, HighLimit = high,
                            Units = txtUnits.Text,
                            Deadband = deadband, Hysteresis = hysteresis
                        };
                        break;
                    }
                    case "AO":
                    {
                        double low = double.Parse(txtLowLimit.Text);
                        double high = double.Parse(txtHighLimit.Text);
                        if (high <= low) { MessageBox.Show("High limit mora biti veći od Low limit."); return; }
                        tag = new AnalogOutput
                        {
                            Name = txtName.Text, Description = txtDesc.Text,
                            IOAddress = txtAddr.Text,
                            InitialValue = double.Parse(txtInitVal.Text),
                            LowLimit = low, HighLimit = high,
                            Units = txtUnits.Text
                        };
                        break;
                    }
                    case "DI":
                    {
                        int scanTime = int.Parse(txtScanTime.Text);
                        if (scanTime <= 0) { MessageBox.Show("Scan time mora biti veći od 0."); return; }
                        tag = new DigitalInput { Name = txtName.Text, Description = txtDesc.Text, IOAddress = txtAddr.Text, ScanTime = scanTime, IsScanning = chkScanning.IsChecked == true };
                        break;
                    }
                    case "DO":
                        tag = new DigitalOutput { Name = txtName.Text, Description = txtDesc.Text, IOAddress = txtAddr.Text, InitialValue = double.Parse(txtInitVal.Text) };
                        break;
                }

                ctx.Tags.Add(tag);
                ctx.SaveChanges();

                if (tag is AnalogInput ai && ai.IsScanning)
                    DataConcentratorManager.Instance.StartTag(ai);
                if (tag is DigitalInput di && di.IsScanning)
                    DataConcentratorManager.Instance.StartTag(di);

                Logger.Log(Logger.LogType.TagCUD, $"Dodat tag {tag.Name} ({type})");
                TagAdded = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.Error, $"Greška dodavanja taga: {ex.Message}");
                MessageBox.Show("Greška: " + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
