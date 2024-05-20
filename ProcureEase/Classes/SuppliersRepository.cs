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

    public static async Task SaveOrganizationToDatabaseAsync(string inn, string kpp, string fullName, string supervisor,
        string contactNumber, string email, int requestTypeId)
    {
        var connectionString = AppSettings.ConnectionString;

        using (var conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();

            const string query = @"
            INSERT INTO suppliers (inn, kpp, organization_full_name, supervisor, email, contact_number, request_type_id)
            VALUES (@inn, @kpp, @fullName, @supervisor, @email, @contactNumber, @requestTypeId)";

            using (var command = new MySqlCommand(query, conn))
            {
                command.Parameters.Add("@inn", MySqlDbType.VarChar).Value = inn;
                command.Parameters.Add("@kpp", MySqlDbType.VarChar).Value = kpp;
                command.Parameters.Add("@fullName", MySqlDbType.VarChar).Value = fullName;
                command.Parameters.Add("@supervisor", MySqlDbType.VarChar).Value = supervisor;
                command.Parameters.Add("@contactNumber", MySqlDbType.VarChar).Value = contactNumber;
                command.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;
                command.Parameters.Add("@requestTypeId", MySqlDbType.Int32).Value = requestTypeId;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}