#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BCrypt.Net;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class RegisterForm
{
    // Строка подключения к БД вынесена в отдельный файл конфигурации
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;

    // Store compiled Regex as static to improve performance
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250)
    );

    private static readonly Regex NameRegex = new(
        @"^[a-zA-Zа-яА-ЯёЁ]+$",
        RegexOptions.Compiled
    );

    private readonly Brush _defaultBorderBrush = Brushes.Gray;

    // Dialog settings can be a field if they are reused across methods.
    private readonly MetroDialogSettings _dialogSettings = new() { AnimateShow = false };
    private readonly List<Control> _inputControls;

    public RegisterForm()
    {
        InitializeComponent();
        _inputControls = new List<Control>
        {
            txtUsername,
            txtPassword,
            txtFirstName,
            txtLastName,
            txtPhoneNumber,
            txtEmail
        };
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Подписка на событие закрытия главного окна
        if (Application.Current.MainWindow != null) Application.Current.MainWindow.Closed += OnMainWindowClosed;
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        await RegisterUserAsync();
    }

    private async Task RegisterUserAsync()
    {
        ResetBorders();

        var errorMessage = ValidateFields();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            await ShowRegistrationError(errorMessage);
            return;
        }

        var registrationSuccess = await TryRegisterUser();
        if (registrationSuccess)
        {
            ClearFields();
            OpenMainForm(txtUsername.Text);
        }
        else
        {
            await ShowRegistrationError("Регистрация не удалась. Пожалуйста, попробуйте позже.");
        }
    }

    private async Task<bool> TryRegisterUser()
    {
        try
        {
            var rowsAffected = await RegisterUser();
            return rowsAffected > 0;
        }
        catch
        {
            // Log the exception details to a file or other logging infrastructure
            await ShowRegistrationError("Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.");
            return false;
        }
    }

    private async Task<int> RegisterUser()
    {
        await using var connection = new MySqlConnection(ConnectionString);
        const string query = """
                             
                                     INSERT INTO users (username, password, first_name, last_name, patronymic, phone_number, email)
                                     VALUES (@username, @password, @firstName, @lastName, @patronymic, @phoneNumber, @email)
                             """;

        await using var command = new MySqlCommand(query, connection);
        // Assuming HashPassword is a method that hashes the password properly.
        command.Parameters.AddWithValue("@username", txtUsername.Text);
        command.Parameters.AddWithValue("@password", HashPassword(txtPassword.Password));
        command.Parameters.AddWithValue("@firstName", txtFirstName.Text.Trim());
        command.Parameters.AddWithValue("@lastName", txtLastName.Text.Trim());
        command.Parameters.AddWithValue("@patronymic",
            string.IsNullOrWhiteSpace(txtPatronymic.Text) ? DBNull.Value : txtPatronymic.Text.Trim());
        command.Parameters.AddWithValue("@phoneNumber", Regex.Replace(txtPhoneNumber.Text, "[^0-9]", ""));
        command.Parameters.AddWithValue("@email", txtEmail.Text);

        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    private async Task ShowRegistrationError(string message)
    {
        await this.ShowMessageAsync("Ошибка регистрации", message, MessageDialogStyle.Affirmative, _dialogSettings);
    }

    private void OpenMainForm(string username)
    {
        Hide();
        var mainForm = new Main(username);
        mainForm.Show();
    }

    private string ValidateFields()
    {
        var errorMessage = new StringBuilder();

        ValidateUsername(errorMessage);
        ValidatePassword(errorMessage);
        ValidateName(txtFirstName, "Имя", errorMessage);
        ValidateName(txtLastName, "Фамилия", errorMessage);
        ValidatePhoneNumber(errorMessage);
        ValidateEmail(errorMessage);

        return errorMessage.ToString();
    }

    private void ValidateUsername(StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            errorMessage.AppendLine("Имя пользователя не может быть пустым.");
            txtUsername.BorderBrush = Brushes.Red;
        }
        else
        {
            txtUsername.BorderBrush = Brushes.Gray; // Or the default color
        }
    }

    private void ValidatePassword(StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(txtPassword.Password))
        {
            errorMessage.AppendLine("Пароль не может быть пустым.");
            txtPassword.BorderBrush = Brushes.Red;
        }
        else
        {
            txtPassword.BorderBrush = Brushes.Gray; // Or the default color
        }
    }

    private static void ValidateName(TextBox textBox, string fieldName, StringBuilder errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(textBox.Text) && IsValidName(textBox.Text))
        {
            textBox.BorderBrush = Brushes.Gray; // Or the default color
            return;
        }

        errorMessage.AppendLine($"{fieldName} не может быть пустым и должно содержать только буквы.");
        textBox.BorderBrush = Brushes.Red;
    }

    private void ValidatePhoneNumber(StringBuilder errorMessage)
    {
        if (txtPhoneNumber.IsMaskCompleted)
        {
            txtPhoneNumber.BorderBrush = Brushes.Gray; // Or the default color
            return;
        }

        errorMessage.AppendLine("Номер телефона не может быть пустым.");
        txtPhoneNumber.BorderBrush = Brushes.Red;
    }

    private void ValidateEmail(StringBuilder errorMessage)
    {
        if (IsValidEmail(txtEmail.Text))
        {
            txtEmail.BorderBrush = Brushes.Gray; // Or the default color
            return;
        }

        errorMessage.AppendLine("Неправильный формат адреса электронной почты.");
        txtEmail.BorderBrush = Brushes.Red;
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }

    private static bool IsValidName(string name)
    {
        return NameRegex.IsMatch(name);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA384);
    }

    private void ResetBorders()
    {
        foreach (var control in _inputControls) control.BorderBrush = _defaultBorderBrush;
    }

    private void ClearFields()
    {
        foreach (var control in _inputControls.OfType<TextBox>()) control.Clear();

        txtPassword.Clear(); // Assuming txtPassword is a PasswordBox and not included in _inputControls
        txtPatronymic.Clear(); // Assuming txtPatronymic is a TextBox
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = (TextBox)sender;
        if (textBox.BorderBrush == Brushes.Red) textBox.BorderBrush = Brushes.Gray;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        if (passwordBox.BorderBrush == Brushes.Red) passwordBox.BorderBrush = Brushes.Gray;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.MainWindow?.Show();
        Hide();
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
}