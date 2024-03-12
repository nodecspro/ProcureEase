using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using MySql.Data.MySqlClient;

namespace ProcureEase;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            var connstring = "Server=localhost; database=procureease; UID=root; password=";
            var conn = new MySqlConnection(connstring);
            conn.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private string HashPassword(string password)
    {
        using (var sha256Hash = SHA256.Create())
        {
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

            var builder = new StringBuilder();
            for (var i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text;
        var password = txtPassword.Password;
        var hashedPassword = HashPassword(password);

        try
        {
            var connstring = "Server=localhost; database=procureease; UID=root; password=";
            using (var conn = new MySqlConnection(connstring))
            {
                conn.Open();

                // Подготовка SQL-запроса для проверки существования пользователя с указанным именем и зашифрованным паролем
                var query = "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);
                    var count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                        MessageBox.Show("Вы успешно авторизованы!");
                    // Дополнительные действия после успешной авторизации, например, переход на другое окно
                    else
                        MessageBox.Show("Неверное имя пользователя или пароль!");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при попытке авторизации: " + ex.Message);
        }
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        // Логика регистрации нового пользователя
    }
}