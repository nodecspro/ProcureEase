#region

using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BCrypt.Net;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class RegisterForm : MetroWindow
{
    // Строка подключения к БД вынесена в отдельный файл конфигурации
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;

    public RegisterForm()
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Подписка на событие закрытия главного окна
        Application.Current.MainWindow.Closed += OnMainWindowClosed;
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
            await this.ShowMessageAsync("Ошибка регистрации", errorMessage, MessageDialogStyle.Affirmative,
                new MetroDialogSettings { AnimateShow = false });
            return;
        }

        try
        {
            var rowsAffected = await RegisterUser();
            if (rowsAffected > 0)
            {
                ClearFields();
                var mainForm = new Main(txtUsername.Text);
                mainForm.Show();
                Hide();
            }
            else
            {
                await this.ShowMessageAsync("Ошибка", "Регистрация не удалась. Пожалуйста, попробуйте позже.");
            }
        }
        catch
        {
            // Показ пользователю общего сообщения об ошибке
            await this.ShowMessageAsync("Ошибка",
                "Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.");
        }
    }

    private async Task<int> RegisterUser()
    {
        using var connection = new MySqlConnection(ConnectionString);
        const string query =
            "INSERT INTO users (username, password, first_name, last_name, patronymic, phone_number, email) " +
            "VALUES (@username, @password, @firstName, @lastName, @patronymic, @phoneNumber, @email)";
        var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", txtUsername.Text);
        command.Parameters.AddWithValue("@password", HashPassword(txtPassword.Password));
        command.Parameters.AddWithValue("@firstName", txtFirstName.Text.Trim());
        command.Parameters.AddWithValue("@lastName", txtLastName.Text.Trim());
        command.Parameters.AddWithValue("@patronymic",
            string.IsNullOrWhiteSpace(txtPatronymic.Text) ? DBNull.Value : txtPatronymic.Text.Trim());

        // Улучшенная обработка номера телефона
        var phoneNumber = Regex.Replace(txtPhoneNumber.Text, "[^0-9]", "");
        command.Parameters.AddWithValue("@phoneNumber", phoneNumber);

        command.Parameters.AddWithValue("@email", txtEmail.Text);

        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    private string ValidateFields()
    {
        var errorMessage = new StringBuilder();

        ValidateUsername(errorMessage);
        ValidatePassword(errorMessage);
        ValidateFirstName(errorMessage);
        ValidateLastName(errorMessage);
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
    }

    private void ValidatePassword(StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(txtPassword.Password))
        {
            errorMessage.AppendLine("Пароль не может быть пустым.");
            txtPassword.BorderBrush = Brushes.Red;
        }
    }

    private void ValidateFirstName(StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(txtFirstName.Text) || !IsValidName(txtFirstName.Text))
        {
            errorMessage.AppendLine("Имя не может быть пустым и должно содержать только буквы.");
            txtFirstName.BorderBrush = Brushes.Red;
        }
    }

    private void ValidateLastName(StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(txtLastName.Text) || !IsValidName(txtLastName.Text))
        {
            errorMessage.AppendLine("Фамилия не может быть пустой и должна содержать только буквы.");
            txtLastName.BorderBrush = Brushes.Red;
        }
    }

    private void ValidatePhoneNumber(StringBuilder errorMessage)
    {
        if (!txtPhoneNumber.IsMaskCompleted)
        {
            errorMessage.AppendLine("Номер телефона не может быть пустым.");
            txtPhoneNumber.BorderBrush = Brushes.Red;
        }
    }

    private void ValidateEmail(StringBuilder errorMessage)
    {
        if (!IsValidEmail(txtEmail.Text))
        {
            errorMessage.AppendLine("Неправильный формат адреса электронной почты.");
            txtEmail.BorderBrush = Brushes.Red;
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        // Use a single regex with optimized timeout and case-insensitive flag
        const string emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
    }

    private static bool IsValidName(string name)
    {
        const string pattern = @"^[a-zA-Zа-яА-ЯёЁ]+$";
        return Regex.IsMatch(name, pattern);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA384);
    }

    private void ResetBorders()
    {
        txtUsername.BorderBrush = Brushes.Gray;
        txtPassword.BorderBrush = Brushes.Gray;
        txtFirstName.BorderBrush = Brushes.Gray;
        txtLastName.BorderBrush = Brushes.Gray;
        txtPhoneNumber.BorderBrush = Brushes.Gray;
        txtEmail.BorderBrush = Brushes.Gray;
    }

    private void ClearFields()
    {
        txtUsername.Clear();
        txtPassword.Clear();
        txtFirstName.Clear();
        txtLastName.Clear();
        txtPatronymic.Clear();
        txtPhoneNumber.Clear();
        txtEmail.Clear();
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
        Application.Current.MainWindow.Show();
        Hide();
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
}