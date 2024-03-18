#region

using System.Globalization;
using System.Net;
using System.Net.Sockets;
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
    private const string ConnectionString = "Server=localhost;Database=procureease;Uid=root;Pwd=;";

    public RegisterForm()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        _ = RegisterUserAsync();
    }

    private async Task RegisterUserAsync()
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
            var rowsAffected = await RegisterUser();
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
            // Consider logging the error here
            MessageBox.Show("Произошла ошибка: " + ex.Message);
        }
    }

    private async Task<int> RegisterUser()
    {
        await using var connection = new MySqlConnection(ConnectionString);
        const string query =
            "INSERT INTO users (username, password, first_name, last_name, patronymic, phone_number, email, role) " +
            "VALUES (@username, @password, @firstName, @lastName, @patronymic, @phoneNumber, @email, 'User')";
        var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", txtUsername.Text);
        var hashedPassword = HashPassword(txtPassword.Password);
        command.Parameters.AddWithValue("@password", hashedPassword);
        command.Parameters.AddWithValue("@firstName", txtFirstName.Text.Trim());
        command.Parameters.AddWithValue("@lastName", txtLastName.Text.Trim());
        command.Parameters.AddWithValue("@patronymic",
            string.IsNullOrWhiteSpace(txtPatronymic.Text) ? DBNull.Value : txtPatronymic.Text.Trim());
        var phoneNumber = new string(Array.FindAll(txtPhoneNumber.Text.ToCharArray(), char.IsDigit));
        command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
        command.Parameters.AddWithValue("@email", txtEmail.Text);

        connection.Open();
        return command.ExecuteNonQuery();
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
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Normalize the domain
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));

            // Examining if it is in the correct email format.
            if (Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
            {
                var domain = email.Split('@')[1];
                var host = Dns.GetHostEntry(domain);

                foreach (var ip in host.AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return true;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }

        return false;
    }

    private static string DomainMapper(Match match)
    {
        // Use IdnMapping class to convert Unicode domain names.
        var idn = new IdnMapping();

        // Pull out and process domain name (throws ArgumentException on invalid)
        var domainName = idn.GetAscii(match.Groups[2].Value);

        return match.Groups[1].Value + domainName;
    }


    private static bool IsValidName(string name)
    {
        var pattern = @"^[a-zA-Zа-яА-ЯёЁ]+$";
        var regex = new Regex(pattern);
        return regex.IsMatch(name);
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
        var mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow?.Show();
        Hide();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}