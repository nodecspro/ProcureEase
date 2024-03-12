#region

using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class MainWindow : Window
{
    private MySqlConnection connection;
    private string database;
    private string password;
    private string server;
    private string uid;

    public MainWindow()
    {
        InitializeComponent();
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

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text;
        var password = txtPassword.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Пожалуйста, введите имя пользователя и пароль.");
            return;
        }

        if (ValidateUser(username, password))
        {
            MessageBox.Show("Авторизация успешна!");
            Hide();

            // Открываем новое окно
            var mainForm = new MainForm();
            mainForm.Show();
        }
        else
        {
            MessageBox.Show("Ошибка авторизации. Проверьте правильность введенных данных.");
        }
    }

    private bool ValidateUser(string username, string password)
    {
        var query = $"SELECT COUNT(*) FROM users WHERE username = '{username}' AND password = '{password}'";

        try
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();

            var cmd = new MySqlCommand(query, connection);
            var count = Convert.ToInt32(cmd.ExecuteScalar());

            return count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при попытке авторизации: {ex.Message}");
            return false;
        }
        finally
        {
            connection.Close();
        }
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