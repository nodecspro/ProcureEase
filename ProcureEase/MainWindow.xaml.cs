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

        MessageBox.Show(ValidateUser(username, password)
            ? "Авторизация успешна!"
            // Здесь может быть переход на другое окно или выполнение других действий после успешной авторизации
            : "Ошибка авторизации. Проверьте правильность введенных данных.");
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

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        // Логика для кнопки "Регистрация"
    }
}