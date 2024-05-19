#region

using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public class SuppliersRepository
{
    private static MySqlConnection GetConnection()
    {
        return new MySqlConnection(AppSettings.ConnectionString);
    }

    public static void SaveOrganizationToDatabase(string inn, string kpp, string fullName, string supervisor,
        string contactNumber, string email)
    {
        using var connection = GetConnection();
        connection.Open();

        const string query = @"
            INSERT INTO suppliers (inn, kpp, organization_full_name, supervisor, email, contact_number)
            VALUES (@inn, @kpp, @fullName, @supervisor, @email, @contactNumber)";

        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.Add("@inn", MySqlDbType.VarChar).Value = inn;
            command.Parameters.Add("@kpp", MySqlDbType.VarChar).Value = kpp;
            command.Parameters.Add("@fullName", MySqlDbType.VarChar).Value = fullName;
            command.Parameters.Add("@supervisor", MySqlDbType.VarChar).Value = supervisor;
            command.Parameters.Add("@contactNumber", MySqlDbType.VarChar).Value = contactNumber;
            command.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;
            command.ExecuteNonQuery();
        }
    }
}