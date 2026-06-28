using System;
using System.Windows;
using DataConcentrator;
using DataConcentrator.Model;

namespace ScadaGUI
{
    public partial class WriteValueWindow : Window
    {
        private readonly Tag tag;

        public WriteValueWindow(Tag tag)
        {
            InitializeComponent();
            this.tag = tag;
            string addr = tag is AnalogOutput ao ? ao.IOAddress : ((DigitalOutput)tag).IOAddress;
            lblInfo.Text = $"Tag: {tag.Name} ({(tag is AnalogOutput ? "AO" : "DO")})\nAdresa: {addr}";
            txtValue.Text = tag is AnalogOutput ao2 ? ao2.InitialValue.ToString() : ((DigitalOutput)tag).InitialValue.ToString();
        }

        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtValue.Text, out double val))
            {
                MessageBox.Show("Unesite validnu numeričku vrijednost.");
                return;
            }

            if (tag is AnalogOutput ao)
            {
                PLC.Instance.SetValue(ao.IOAddress, val);
                ao.InitialValue = val;
            }
            else if (tag is DigitalOutput dout)
            {
                if (val != 0 && val != 1) { MessageBox.Show("Digitalni izlaz može biti samo 0 ili 1."); return; }
                PLC.Instance.SetValue(dout.IOAddress, val);
                dout.InitialValue = val;
            }

            ContextClass.Instance.SaveChanges();
            Logger.Log(Logger.LogType.TagUpdate, $"Upisana vrijednost {val} u tag {tag.Name}");
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
