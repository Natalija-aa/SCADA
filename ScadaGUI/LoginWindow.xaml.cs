using System.Windows;
using DataConcentrator;

namespace ScadaGUI
{
    public partial class LoginWindow : Window
    {
        public bool LoggedIn { get; private set; }
        public string Username { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = pwdPassword.Password;

            // polja ne smiju biti prazna
            if (string.IsNullOrWhiteSpace(username))
            {
                lblError.Text = "Unesite korisničko ime.";
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Unesite lozinku.";
                return;
            }

            Username = username;
            LoggedIn = true;
            Close();
        }

        // ako se pritisne cance zatvara se aplikacija
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
