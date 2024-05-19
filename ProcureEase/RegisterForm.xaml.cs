#region

using System.Configuration;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
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
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;

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
    private readonly MetroDialogSettings _dialogSettings = new() { AnimateShow = false, AnimateHide = false };
    private readonly List<Control> _inputControls;

    public RegisterForm()
    {
        InitializeComponent();

        _inputControls = new List<Control>
        {
            TxtUsername,
            TxtPassword,
            TxtFirstName,
            TxtLastName,
            TxtPhoneNumber,
            TxtEmail,
            TxtInviteCode
        };
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        if (Application.Current.MainWindow != null)
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
            await ShowMessageAsync("Ошибка регистрации", errorMessage);
            return;
        }

        if (await TryRegisterUser())
        {
            OpenMainForm(TxtUsername.Text);
            ClearFields();
        }
        else
        {
            await ShowMessageAsync("Ошибка регистрации", "Регистрация не удалась. Пожалуйста, попробуйте позже.");
        }
    }

    private async Task<bool> TryRegisterUser()
    {
        try
        {
            return await RegisterUser() > 0;
        }
        catch
        {
            await ShowMessageAsync("Ошибка регистрации",
                "Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.");
            return false;
        }
    }

    private async Task<int> RegisterUser()
    {
        var inviteData = await ValidateAndFetchInviteData(TxtInviteCode.Text);
        if (inviteData == null)
        {
            MessageBox.Show("Неправильный или истекший код приглашения.");
            return 0;
        }

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string userInsertQuery = """
                                               INSERT INTO users (username, password, first_name, last_name, patronymic, phone_number, email, role_id, organization_id)
                                               VALUES (@username, @password, @firstName, @lastName, @patronymic, @phoneNumber, @email, @roleId, @organizationId)
                                           """;

            await using (var userInsertCommand = new MySqlCommand(userInsertQuery, connection))
            {
                userInsertCommand.Transaction = transaction;
                userInsertCommand.Parameters.AddWithValue("@username", TxtUsername.Text);
                userInsertCommand.Parameters.AddWithValue("@password", HashPassword(TxtPassword.Password));
                userInsertCommand.Parameters.AddWithValue("@firstName", TxtFirstName.Text.Trim());
                userInsertCommand.Parameters.AddWithValue("@lastName", TxtLastName.Text.Trim());
                userInsertCommand.Parameters.AddWithValue("@patronymic",
                    string.IsNullOrWhiteSpace(TxtPatronymic.Text) ? DBNull.Value : TxtPatronymic.Text.Trim());
                userInsertCommand.Parameters.AddWithValue("@phoneNumber",
                    Regex.Replace(TxtPhoneNumber.Text, "[^0-9]", ""));
                userInsertCommand.Parameters.AddWithValue("@email", TxtEmail.Text);
                userInsertCommand.Parameters.AddWithValue("@roleId", inviteData.Value.RoleId);
                userInsertCommand.Parameters.AddWithValue("@organizationId", inviteData.Value.OrganizationId);

                await userInsertCommand.ExecuteNonQueryAsync();
            }

            const string inviteDeleteQuery = "DELETE FROM invitation_codes WHERE code = @code";

            await using (var inviteDeleteCommand = new MySqlCommand(inviteDeleteQuery, connection))
            {
                inviteDeleteCommand.Transaction = transaction;
                inviteDeleteCommand.Parameters.AddWithValue("@code", TxtInviteCode.Text);
                await inviteDeleteCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return 1;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
            return 0;
        }
    }

    private static async Task<(int RoleId, int OrganizationId)?> ValidateAndFetchInviteData(string inviteCode)
    {
        if (!IsValidInviteCode(inviteCode)) return null;

        await using var connection = new MySqlConnection(ConnectionString);
        const string inviteQuery =
            "SELECT role_id, organization_id FROM invitation_codes WHERE code = @code AND (expiration_date IS NULL OR expiration_date > NOW())";

        await using var command = new MySqlCommand(inviteQuery, connection);
        command.Parameters.AddWithValue("@code", inviteCode);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var roleId = reader.GetInt32("role_id");
            var organizationId = reader.GetInt32("organization_id");
            return (roleId, organizationId);
        }

        return null;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, _dialogSettings);
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
        ValidateUsername(TxtUsername, "Имя пользователя", errorMessage);
        ValidatePassword(TxtPassword, "Пароль", errorMessage);
        ValidateName(TxtFirstName, "Имя", errorMessage);
        ValidateName(TxtLastName, "Фамилия", errorMessage);
        ValidatePhoneNumber(errorMessage);
        ValidateEmail(errorMessage);
        ValidateInviteCode(errorMessage);
        return errorMessage.ToString();
    }

    private void ValidateUsername(Control control, string fieldName, StringBuilder errorMessage)
    {
        const string pattern = @"^[a-zA-Z0-9]+$";
        var input = control switch
        {
            TextBox tb => tb.Text,
            PasswordBox pb => pb.Password,
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage.AppendLine($"{fieldName} не может быть пустым.");
            control.BorderBrush = Brushes.Red;
        }
        else if (!Regex.IsMatch(input, pattern))
        {
            errorMessage.AppendLine($"{fieldName} может содержать только английские буквы.");
            control.BorderBrush = Brushes.Red;
        }
        else
        {
            control.BorderBrush = _defaultBorderBrush;
        }
    }

    private void ValidatePassword(Control control, string fieldName, StringBuilder errorMessage)
    {
        const string pattern = @"^[a-zA-Z0-9!@#$%^&*()_+\-=]+$";
        var input = control switch
        {
            TextBox tb => tb.Text,
            PasswordBox pb => pb.Password,
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage.AppendLine($"{fieldName} не может быть пустым.");
            control.BorderBrush = Brushes.Red;
        }
        else if (!Regex.IsMatch(input, pattern))
        {
            errorMessage.AppendLine(
                $"{fieldName} может содержать только английские буквы, цифры и базовые специальные символы.");
            control.BorderBrush = Brushes.Red;
        }
        else
        {
            control.BorderBrush = _defaultBorderBrush;
        }
    }

    private static void ValidateName(TextBox textBox, string fieldName, StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(textBox.Text) || !IsValidName(textBox.Text))
        {
            errorMessage.AppendLine($"{fieldName} не может быть пустым и должно содержать только буквы.");
            textBox.BorderBrush = Brushes.Red;
        }
        else
        {
            textBox.BorderBrush = Brushes.Gray;
        }
    }

    private void ValidatePhoneNumber(StringBuilder errorMessage)
    {
        if (!TxtPhoneNumber.IsMaskCompleted)
        {
            errorMessage.AppendLine("Номер телефона не может быть пустым.");
            TxtPhoneNumber.BorderBrush = Brushes.Red;
        }
        else
        {
            TxtPhoneNumber.BorderBrush = _defaultBorderBrush;
        }
    }

    private void ValidateEmail(StringBuilder errorMessage)
    {
        if (!IsValidEmail(TxtEmail.Text))
        {
            errorMessage.AppendLine("Неправильный формат адреса электронной почты.");
            TxtEmail.BorderBrush = Brushes.Red;
        }
        else
        {
            TxtEmail.BorderBrush = _defaultBorderBrush;
        }
    }

    private void ValidateInviteCode(StringBuilder errorMessage)
    {
        if (string.IsNullOrWhiteSpace(TxtInviteCode.Text))
        {
            errorMessage.AppendLine("Поле кода приглашения не может быть пустым.");
            TxtInviteCode.BorderBrush = Brushes.Red;
        }
        else if (!IsValidInviteCode(TxtInviteCode.Text))
        {
            errorMessage.AppendLine("Неправильный код приглашения.");
            TxtInviteCode.BorderBrush = Brushes.Red;
        }
        else
        {
            TxtInviteCode.BorderBrush = _defaultBorderBrush;
        }
    }

    private static bool IsValidInviteCode(string code)
    {
        return Regex.IsMatch(code, @"^[a-zA-Z0-9]{1,8}$");
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
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = (TextBox)sender;
        if (textBox.BorderBrush == Brushes.Red) textBox.BorderBrush = _defaultBorderBrush;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        if (passwordBox.BorderBrush == Brushes.Red) passwordBox.BorderBrush = _defaultBorderBrush;
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