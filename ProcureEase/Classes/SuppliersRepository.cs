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
        await using var conn = GetConnection();
        await conn.OpenAsync();

        const string query = """
                             INSERT INTO suppliers (
                               inn, kpp, organization_full_name,
                               supervisor, email, contact_number,
                               request_type_id
                             )
                             VALUES
                               (
                                 @inn, @kpp, @fullName, @supervisor,
                                 @email, @contactNumber, @requestTypeId
                               )
                             """;

        await using var command = new MySqlCommand(query, conn);
        command.Parameters.Add("@inn", MySqlDbType.VarChar).Value = inn;
        command.Parameters.Add("@kpp", MySqlDbType.VarChar).Value = kpp;
        command.Parameters.Add("@fullName", MySqlDbType.VarChar).Value = fullName;
        command.Parameters.Add("@supervisor", MySqlDbType.VarChar).Value = supervisor;
        command.Parameters.Add("@contactNumber", MySqlDbType.VarChar).Value = contactNumber;
        command.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;
        command.Parameters.Add("@requestTypeId", MySqlDbType.Int32).Value = requestTypeId;
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<Supplier> GetSupplierByUserIdAsync(int userId)
    {
        const string query = """
                             SELECT
                               s.supplier_id,
                               s.inn,
                               s.kpp,
                               s.organization_full_name,
                               s.supervisor,
                               s.email,
                               s.contact_number
                             FROM
                               suppliers s
                               JOIN users u ON u.organization_id = s.supplier_id
                             WHERE
                               u.user_id = @userId
                             """;

        try
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var command = new MySqlCommand(query, conn);
            command.Parameters.AddWithValue("@userId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return new Supplier
                {
                    SupplierId = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                    Inn = reader.GetString(reader.GetOrdinal("inn")),
                    Kpp = reader.GetString(reader.GetOrdinal("kpp")),
                    OrganizationFullName = reader.GetString(reader.GetOrdinal("organization_full_name")),
                    Supervisor = reader.GetString(reader.GetOrdinal("supervisor")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    ContactNumber = reader.GetString(reader.GetOrdinal("contact_number"))
                };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }

        return null;
    }
}