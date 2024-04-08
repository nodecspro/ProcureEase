#region

using System.Configuration;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class MainWindow
{
    // Connection string moved to a separate configuration file
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;

    private readonly MySqlConnection connection;

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Initialize connection with connection string from configuration
        connection = new MySqlConnection(ConnectionString);

        // Subscribe to main window closed event
        Application.Current.MainWindow.Closed += OnMainWindowClosed;
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        await LoginUserAsync();
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
                var mainForm = new Main(username);
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
        const string query = "SELECT password FROM users WHERE username = @username";

        await using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@username", username);

        try
        {
            await connection.OpenAsync();
            var hashedPasswordFromDb = await cmd.ExecuteScalarAsync() as string;
            return hashedPasswordFromDb != null &&
                   BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPasswordFromDb);
        }
        catch (MySqlException ex)
        {
            await this.ShowMessageAsync("Ошибка авторизации", $"Ошибка базы данных: {ex.Message}");
            return false;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
    {
        var registerForm = new RegisterForm();
        registerForm.Show();
        Hide();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        // Close all open windows
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}