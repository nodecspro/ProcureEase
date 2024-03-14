#region

using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class MainWindow : MetroWindow
{
    private MySqlConnection connection;
    private string database;
    private string password;
    private string server;
    private string uid;

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        server = "localhost";
        database = "procureease";
        uid = "root";
        password = "";

        var connectionString = $"SERVER={server};DATABASE={database};UID={uid};PASSWORD={password};";

        connection = new MySqlConnection(connectionString);
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text;
        var password = txtPassword.Password;

        var dialogSettings = new MetroDialogSettings
        {
            AnimateShow = false // Отключить анимацию
        };

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            //MessageBox.Show("Пожалуйста, введите имя пользователя и пароль.");
            await this.ShowMessageAsync("Ошибка авторизации", "Пожалуйста, введите имя пользователя и пароль.",
                MessageDialogStyle.Affirmative,
                dialogSettings);

            return;
        }

        if (ValidateUser(username, password))
        {
            Hide();
            // Открываем новое окно
            var mainForm = new Main();
            mainForm.Show();
        }
        else
        {
            //MessageBox.Show("Ошибка авторизации. Проверьте правильность введенных данных.");
            await this.ShowMessageAsync("Ошибка авторизации", "Проверьте правильность введенных данных.",
                MessageDialogStyle.Affirmative,
                dialogSettings);
        }
    }

    private bool ValidateUser(string username, string password)
    {
        var hashedPassword = HashPassword(password);

        var query = $"SELECT COUNT(*) FROM users WHERE username = '{username}' AND password = '{hashedPassword}'";

        try
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();

            var cmd = new MySqlCommand(query, connection);
            var count = Convert.ToInt32(cmd.ExecuteScalar());

            return count > 0;
        }
        catch
        {
            //MessageBox.Show($"Ошибка при попытке авторизации: {ex.Message}");
            this.ShowMessageAsync("Ошибка авторизации", "Ошибка при попытке авторизации");
            return false;
        }
        finally
        {
            connection.Close();
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
    {
        // Открываем форму регистрации (RegisterForm.xaml)
        var registerForm = new RegisterForm();
        registerForm.Show();

        // Скрываем текущее окно авторизации
        Hide();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}