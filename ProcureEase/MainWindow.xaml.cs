#region

using System.Data;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class MainWindow : MetroWindow
{
    private const string Database = "procureease";
    private const string Server = "localhost";
    private new const string Uid = "root";
    private const string Password = "";
    private readonly MySqlConnection connection;

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
        connection = new MySqlConnection($"SERVER={Server};DATABASE={Database};UID={Uid};PASSWORD={Password};");
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        LoginUserAsync();
    }

    private async Task LoginUserAsync()
    {
        var username = txtUsername.Text;
        var password = txtPassword.Password;

        var dialogSettings = new MetroDialogSettings { AnimateShow = false };

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await this.ShowMessageAsync("Ошибка авторизации", "Пожалуйста, введите имя пользователя и пароль.",
                MessageDialogStyle.Affirmative, dialogSettings);
            return;
        }

        try
        {
            if (await ValidateUser(username, password))
            {
                Hide();
                var mainForm = new Main();
                mainForm.Show();
            }
            else
            {
                await this.ShowMessageAsync("Ошибка авторизации", "Проверьте правильность введенных данных.",
                    MessageDialogStyle.Affirmative, dialogSettings);
            }
        }
        catch (Exception ex)
        {
            await this.ShowMessageAsync("Ошибка авторизации", $"Ошибка при попытке авторизации: {ex.Message}",
                MessageDialogStyle.Affirmative, dialogSettings);
        }
    }


    private async Task<bool> ValidateUser(string username, string password)
    {
        var query = "SELECT password FROM users WHERE username = @username";

        await using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@username", username);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var hashedPasswordFromDb = await cmd.ExecuteScalarAsync() as string;
        return hashedPasswordFromDb != null && // Если пользователь с таким именем не найден
               BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPasswordFromDb);
    }


    private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
    {
        var registerForm = new RegisterForm();
        registerForm.Show();
        Hide();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}