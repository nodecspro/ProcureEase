#region

using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

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
                Email = reader.GetString("email"),
                FirstName = reader.GetString("first_name"),
                LastName = reader.GetString("last_name"),
                Patronymic = reader.IsDBNull(reader.GetOrdinal("patronymic"))
                    ? null
                    : reader.GetString("patronymic"),
                PhoneNumber = reader.GetString("phone_number")
            };

        return null;
    }
}