using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ScadaGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new LoginWindow();
            login.ShowDialog();
            if (!login.LoggedIn)
            {
                Shutdown();
                return;
            }

            ShutdownMode = ShutdownMode.OnLastWindowClose;
            DataConcentrator.Logger.Log(DataConcentrator.Logger.LogType.Login, $"Korisnik '{login.Username}' se prijavio.");
            new MainWindow().Show();
        }
    }
}
