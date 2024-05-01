#region

using System.Data;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public static class RequestRepository
{
    private static MySqlConnection GetConnection()
    {
        return new MySqlConnection(AppSettings.ConnectionString);
    }

    public static async Task<IEnumerable<Request>> GetUserRequestsAsync(int userId)
    {
        var requests = new List<Request>();
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query = """
                                         SELECT r.request_id, r.request_name, rt.name as request_type,
                                                rs.name as request_status, r.notes
                                         FROM requests r
                                         JOIN request_type rt ON r.request_type_id = rt.idRequestType
                                         JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                                         WHERE r.user_id = @userId
                             """;

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var request = new Request
            {
                RequestId = reader.GetInt32("request_id"),
                RequestName = reader.GetString("request_name"),
                RequestType = reader.GetString("request_type"),
                RequestStatus = reader.GetString("request_status"),
                Notes = reader.GetString("notes"),
                RequestFiles = await GetRequestFilesAsync(reader.GetInt32("request_id"))
            };
            requests.Add(request);
        }

        return requests;
    }

    private static async Task<List<RequestFile>> GetRequestFilesAsync(int requestId)
    {
        var files = new List<RequestFile>();
        await using var connection = GetConnection();
        await connection.OpenAsync();
        const string query = "SELECT file_name FROM request_files WHERE request_id = @RequestId";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestId", requestId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            files.Add(new RequestFile
            {
                FileName = reader.GetString("file_name")
            });

        return files;
    }

    public static async Task<int> AddRequestAsync(Request request)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query = @"
            INSERT INTO requests (request_name, notes, user_id, request_status_id, request_type_id)
            VALUES(@RequestName, @Notes, @UserId, @RequestStatusId, @RequestTypeId);
            SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestName", request.RequestName);
        command.Parameters.AddWithValue("@Notes", request.Notes);
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@RequestStatusId", await GetRequestStatusIdAsync("В обработке"));
        command.Parameters.AddWithValue("@RequestTypeId", await GetRequestTypeIdAsync(request.RequestType));

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public static async Task AddRequestFilesAsync(int requestId, List<RequestFile> requestFiles)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query =
            "INSERT INTO request_files (request_id, file_name, file_data) VALUES(@RequestId, @FileName, @FileData)";
        await using var command = new MySqlCommand(query, connection);

        foreach (var requestFile in requestFiles)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@RequestId", requestId);
            command.Parameters.AddWithValue("@FileName", requestFile.FileName);
            command.Parameters.AddWithValue("@FileData", requestFile.FileData);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<int> GetRequestStatusIdAsync(string requestStatusName)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query = "SELECT idRequestStatus FROM request_status WHERE name = @RequestStatusName";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestStatusName", requestStatusName);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> GetRequestTypeIdAsync(string requestTypeName)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query = "SELECT idRequestType FROM request_type WHERE name = @RequestTypeName";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestTypeName", requestTypeName);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}