#region

using System.Net.Mail;
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
    private const string ConnectionString = "Server=localhost;Database=procureease;Uid=root;Pwd=;";

    public RegisterForm()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        ResetBorders();

        var errorMessage = ValidateFields();
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            await this.ShowMessageAsync("Ошибка регистрации", errorMessage, MessageDialogStyle.Affirmative,
                new MetroDialogSettings { AnimateShow = false });
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(ConnectionString);
            const string query =
                "INSERT INTO users (username, password, first_name, last_name, patronymic, phone_number, email, role) " +
                "VALUES (@username, @password, @firstName, @lastName, @patronymic, @phoneNumber, @email, 'User')";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", txtUsername.Text);
            var hashedPassword = HashPassword(txtPassword.Password);
            command.Parameters.AddWithValue("@password", hashedPassword);
            command.Parameters.AddWithValue("@firstName", txtFirstName.Text);
            command.Parameters.AddWithValue("@lastName", txtLastName.Text);
            command.Parameters.AddWithValue("@patronymic",
                string.IsNullOrWhiteSpace(txtPatronymic.Text) ? DBNull.Value : txtPatronymic.Text);
            var phoneNumber = new string(Array.FindAll(txtPhoneNumber.Text.ToCharArray(), char.IsDigit));
            command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
            command.Parameters.AddWithValue("@email", txtEmail.Text);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                ClearFields();
                var mainForm = new Main();
                mainForm.Show();
                Hide();
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

    private string ValidateFields()
    {
        var errorMessage = "";

        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            errorMessage += "Имя пользователя не может быть пустым.\n";
            txtUsername.BorderBrush = Brushes.Red;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Password))
        {
            errorMessage += "Пароль не может быть пустым.\n";
            txtPassword.BorderBrush = Brushes.Red;
        }

        if (string.IsNullOrWhiteSpace(txtFirstName.Text) || !IsValidName(txtFirstName.Text))
        {
            errorMessage += "Имя не может быть пустым и должно содержать только буквы.\n";
            txtFirstName.BorderBrush = Brushes.Red;
        }

        if (string.IsNullOrWhiteSpace(txtLastName.Text) || !IsValidName(txtLastName.Text))
        {
            errorMessage += "Фамилия не может быть пустой и должна содержать только буквы.\n";
            txtLastName.BorderBrush = Brushes.Red;
        }

        if (!txtPhoneNumber.IsMaskCompleted)
        {
            errorMessage += "Номер телефона не может быть пустым.\n";
            txtPhoneNumber.BorderBrush = Brushes.Red;
        }

        if (!IsValidEmail(txtEmail.Text))
        {
            errorMessage += "Неправильный формат адреса электронной почты.\n";
            txtEmail.BorderBrush = Brushes.Red;
        }

        return errorMessage;
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

    private static bool IsValidName(string name)
    {
        var pattern = @"^[a-zA-Zа-яА-ЯёЁ]+$";
        var regex = new Regex(pattern);
        return regex.IsMatch(name);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, hashType:HashType.SHA384);
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
        var mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow?.Show();
        Hide();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}