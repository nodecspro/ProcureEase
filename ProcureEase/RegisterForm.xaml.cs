#region

using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class RegisterForm : MetroWindow
{
    #region Constants

    private const string ConnectionString = "Server=localhost;Database=procureease;Uid=root;Pwd=;";

    #endregion

    #region Constructor

    public RegisterForm()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
    }

    #endregion

    #region Event Handlers

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (Window window in Application.Current.Windows) window.Close();
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        ResetBorders(); // Сброс всех границ

        var errorMessage = "";

        // Проверка имени пользователя
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            errorMessage += "Имя пользователя не может быть пустым.\n";
            txtUsername.BorderBrush = Brushes.Red;
        }

        // Проверка пароля
        if (string.IsNullOrWhiteSpace(txtPassword.Password))
        {
            errorMessage += "Пароль не может быть пустым.\n";
            txtPassword.BorderBrush = Brushes.Red;
        }

        // Проверка имени
        if (string.IsNullOrWhiteSpace(txtFirstName.Text) || !IsValidName(txtFirstName.Text))
        {
            errorMessage += "Имя не может быть пустым и должно содержать только буквы.\n";
            txtFirstName.BorderBrush = Brushes.Red;
        }

        // Проверка фамилии
        if (string.IsNullOrWhiteSpace(txtLastName.Text) || !IsValidName(txtLastName.Text))
        {
            errorMessage += "Фамилия не может быть пустой и должна содержать только буквы.\n";
            txtLastName.BorderBrush = Brushes.Red;
        }

        // Проверка номера телефона
        if (!txtPhoneNumber.IsMaskCompleted)
        {
            errorMessage += "Номер телефона не может быть пустым.\n";
            txtPhoneNumber.BorderBrush = Brushes.Red;
        }

        // Проверка адреса электронной почты
        if (!IsValidEmail(txtEmail.Text))
        {
            errorMessage += "Неправильный формат адреса электронной почты.\n";
            txtEmail.BorderBrush = Brushes.Red;
        }

        // Если есть ошибки, показать сообщение
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            var dialogSettings = new MetroDialogSettings
            {
                AnimateShow = false // Отключить анимацию
            };

            // Отображение сообщения об ошибке без анимации
            await this.ShowMessageAsync("Ошибка регистрации", errorMessage, MessageDialogStyle.Affirmative,
                dialogSettings);
            return;
        }

        // Если ошибок нет, продолжить регистрацию
        try
        {
            // Использование оператора using для правильного освобождения ресурсов
            await using var connection = new MySqlConnection(ConnectionString);
            const string query =
                "INSERT INTO users (username, password, first_name, last_name, patronymic, phone_number, email, role) " +
                "VALUES (@username, @password, @firstName, @lastName, @patronymic, @phoneNumber, @email, 'User')";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", txtUsername.Text);
            // Хеширование пароля перед сохранением
            var hashedPassword = HashPassword(txtPassword.Password);
            command.Parameters.AddWithValue("@password", hashedPassword);
            command.Parameters.AddWithValue("@firstName", txtFirstName.Text);
            command.Parameters.AddWithValue("@lastName", txtLastName.Text);
            command.Parameters.AddWithValue("@patronymic",
                string.IsNullOrWhiteSpace(txtPatronymic.Text) ? DBNull.Value : txtPatronymic.Text);
            // Извлечение только цифр из номера телефона
            var phoneNumber = new string(Array.FindAll(txtPhoneNumber.Text.ToCharArray(), char.IsDigit));
            command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
            command.Parameters.AddWithValue("@email", txtEmail.Text);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                await this.ShowMessageAsync("Успех", "Регистрация прошла успешно!");
                ClearFields();
            }
            else
            {
                await this.ShowMessageAsync("Ошибка", "Регистрация не удалась. Пожалуйста, попробуйте позже.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Произошла ошибка: " + ex.Message);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void ResetBorders()
    {
        // Сброс всех границ
        txtUsername.BorderBrush = Brushes.Gray;
        txtPassword.BorderBrush = Brushes.Gray;
        txtFirstName.BorderBrush = Brushes.Gray;
        txtLastName.BorderBrush = Brushes.Gray;
        txtPhoneNumber.BorderBrush = Brushes.Gray;
        txtEmail.BorderBrush = Brushes.Gray;
    }

    private static bool IsValidName(string name)
    {
        // Паттерн для проверки, содержит ли строка только буквы английского и русского алфавитов
        var pattern = @"^[a-zA-Zа-яА-Я]+$";
        var regex = new Regex(pattern);
        return regex.IsMatch(name);
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

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in hashedBytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
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
        var mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow?.Show();
        Hide();
    }

    #endregion
}