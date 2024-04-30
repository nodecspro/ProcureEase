#region

using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public static class RequestRepository
{
    public static IEnumerable<Request> GetUserRequests(int userId)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();

        const string query = """
                             SELECT r.request_id, r.request_name, rt.name as request_type,
                                             rs.name as request_status, r.notes
                                             FROM requests r
                                             JOIN request_type rt ON r.request_type_id = rt.idRequestType
                                             JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                                             WHERE r.user_id = @userId
                             """;

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        var requests = new List<Request>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var request = new Request
            {
                RequestId = reader.GetInt32("request_id"),
                RequestName = reader.GetString("request_name"),
                RequestType = reader.GetString("request_type"),
                RequestStatus = reader.GetString("request_status"),
                Notes = reader.GetString("notes")
            };
            request.RequestFiles = GetRequestFiles(request.RequestId);

            requests.Add(request);
        }

        Console.WriteLine(requests);
        return requests;
    }

    // Новый метод для получения списка файлов по ID заявки
    private static List<RequestFile> GetRequestFiles(int requestId)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();
        const string query = "SELECT file_name FROM request_files WHERE request_id = @RequestId";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestId", requestId);
        var files = new List<RequestFile>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            files.Add(new RequestFile
            {
                FileName = reader.GetString("file_name")
            });

        return files;
    }

    public static int AddRequest(Request request)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();

        const string query =
            """
            INSERT INTO requests (request_name, notes, user_id, request_status_id, request_type_id)
                        VALUES(@RequestName, @Notes, @UserId, @RequestStatusId, @RequestTypeId);
                        SELECT LAST_INSERT_ID();
                        
            """;

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestName", request.RequestName);
        command.Parameters.AddWithValue("@Notes", request.Notes);
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@RequestStatusId", GetRequestStatusId("В обработке"));
        command.Parameters.AddWithValue("@RequestTypeId", GetRequestTypeId(request.RequestType));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public static void AddRequestFiles(int requestId, List<RequestFile> requestFiles)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();

        const string query = """
                             INSERT INTO request_files (request_id, file_name, file_data)
                                         VALUES(@RequestId, @FileName, @FileData)
                                         
                             """;

        foreach (var requestFile in requestFiles)
        {
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@RequestId", requestId);
            command.Parameters.AddWithValue("@FileName", requestFile.FileName);
            command.Parameters.AddWithValue("@FileData", requestFile.FileData);

            command.ExecuteNonQuery();
        }
    }

    private static int GetRequestStatusId(string requestStatusName)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();

        const string query = "SELECT idRequestStatus FROM request_status WHERE name = @RequestStatusName";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestStatusName", requestStatusName);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int GetRequestTypeId(string? requestTypeName)
    {
        using var connection = new MySqlConnection(AppSettings.ConnectionString);
        connection.Open();

        const string query = "SELECT idRequestType FROM request_type WHERE name = @RequestTypeName";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestTypeName", requestTypeName);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}