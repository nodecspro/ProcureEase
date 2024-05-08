#region

using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public static class UserRepository
{
    public static User? GetUserByUsername(string username)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();
        const string query = "SELECT * FROM users WHERE username = @username";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", username);

        using var reader = command.ExecuteReader();
        if (reader.Read())
            return new User
            {
                UserId = reader.GetInt32("user_id"),
                Username = reader.GetString("username"),
                RoleId = reader.GetInt32("role_id"),
                Email = reader.GetString("email"),
                FirstName = reader.GetString("first_name"),
                LastName = reader.GetString("last_name"),
                Patronymic = reader.IsDBNull(reader.GetOrdinal("patronymic"))
                    ? null
                    : reader.GetString("patronymic"),
                PhoneNumber = FormatPhoneNumber(reader.GetString("phone_number")) // Форматируем номер
            };

        return null;
    }

    private static string FormatPhoneNumber(string rawNumber)
    {
        if (string.IsNullOrEmpty(rawNumber) || rawNumber.Length != 11)
            return rawNumber; // Возвращаем как есть, если данные некорректны

        return
            $"+7({rawNumber.Substring(1, 3)}){rawNumber.Substring(4, 3)}-{rawNumber.Substring(7, 2)}-{rawNumber.Substring(9, 2)}";
    }

    public static async Task UpdateUser(User user)
    {
        await using var connection = new MySqlConnection(AppSettings.ConnectionString);
        await connection.OpenAsync();
        var command = new MySqlCommand(
            "UPDATE users SET first_name = @FirstName, last_name = @LastName, patronymic = @Patronymic, phone_number = @PhoneNumber, email = @Email WHERE user_id = @UserId",
            connection);

        // Очищаем номер телефона от нечисловых символов перед добавлением его в команду
        var cleanPhoneNumber = Regex.Replace(user.PhoneNumber, "[^0-9]", "");

        command.Parameters.AddWithValue("@FirstName", user.FirstName);
        command.Parameters.AddWithValue("@LastName", user.LastName);
        command.Parameters.AddWithValue("@Patronymic", user.Patronymic);
        command.Parameters.AddWithValue("@PhoneNumber", cleanPhoneNumber);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@UserId", user.UserId);

        await command.ExecuteNonQueryAsync();
    }
}