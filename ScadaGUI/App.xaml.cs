using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ScadaGUI
{
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // potrebno da se LogWindow zatvori prije MainWindowa
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new LoginWindow();
            login.ShowDialog(); // kod se nece nastaviti dok se prozor ne zatvori
            if (!login.LoggedIn)
            {
                Shutdown(); //ako je pritisnut cancle smo zatvori
                return;
            }

            ShutdownMode = ShutdownMode.OnLastWindowClose;  // main se zatvara - gotov rad aplikacije
            // pisem ko se kad prijavio
            DataConcentrator.Logger.Log(DataConcentrator.Logger.LogType.Login, $"Korisnik '{login.Username}' se prijavio.");   
            new MainWindow().Show();
        }
    }
}
