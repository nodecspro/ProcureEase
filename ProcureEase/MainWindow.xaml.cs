#region

using System;
using System.Configuration;
using System.Threading.Tasks;
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

    private readonly MySqlConnection _connection;

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Initialize connection with connection string from configuration
        _connection = new MySqlConnection(ConnectionString);

        // Subscribe to main window closed event
        if (Application.Current.MainWindow != null) Application.Current.MainWindow.Closed += OnMainWindowClosed;
    }

// Consider moving these dialog settings to a class field if they don't change.
    private readonly MetroDialogSettings _dialogSettings = new MetroDialogSettings { AnimateShow = false };

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        await LoginUserAsync();
    }

    private async Task LoginUserAsync()
    {
        var username = txtUsername.Text;
        var password = txtPassword.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await this.ShowLoginError("Пожалуйста, введите имя пользователя и пароль.");
            return;
        }

        var loginSuccess = await ValidateAndLoginUser(username, password);
        if (!loginSuccess)
        {
            await this.ShowLoginError("Проверьте правильность введенных данных.");
        }
    }

    private async Task<bool> ValidateAndLoginUser(string username, string password)
    {
        var isValidUser = false;
        try
        {
            isValidUser = await ValidateUser(username, password);
            if (isValidUser)
            {
                OpenMainForm(username);
            }
        }
        catch (Exception ex)
        {
            await this.ShowLoginError($"Ошибка при попытке авторизации: {ex.Message}");
        }

        return isValidUser;
    }

    private void OpenMainForm(string username)
    {
        Hide();
        var mainForm = new Main(username);
        mainForm.Show();
    }

    private async Task ShowLoginError(string message)
    {
        await this.ShowMessageAsync("Ошибка авторизации", message, MessageDialogStyle.Affirmative, _dialogSettings);
    }

    private async Task<bool> ValidateUser(string username, string password)
    {
        const string query = "SELECT password FROM users WHERE username = @username";

        await using var cmd = new MySqlCommand(query, _connection);
        cmd.Parameters.AddWithValue("@username", username);

        try
        {
            await _connection.OpenAsync();
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
            await _connection.CloseAsync();
        }
    }

    private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
    {
        var registerForm = new RegisterForm();
        registerForm.Show();
        Hide();
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
}