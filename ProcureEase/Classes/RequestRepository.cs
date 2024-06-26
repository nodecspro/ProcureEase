﻿#region

using System.Collections.ObjectModel;
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

    public static async Task<IEnumerable<Request>> GetUserRequestsAsync(User user)
    {
        var requests = new List<Request>();
        await using var connection = GetConnection();
        await connection.OpenAsync();

        string query;

        // Проверяем RoleId пользователя
        switch (user.RoleId)
        {
            case 1:
                // Для роли 1 возвращаем все заявки
                query = """
                        SELECT
                          r.request_id,
                          r.request_name,
                          rt.name as request_type,
                          rs.name as request_status,
                          r.notes,
                          r.decline_reason
                        FROM
                          requests r
                          JOIN request_type rt ON r.request_type_id = rt.idRequestType
                          JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                        """;
                break;
            case 2:
                // Для роли 2 возвращаем заявки, где idRequestStatus = 2
                query = """
                        SELECT
                          r.request_id,
                          r.request_name,
                          rt.name as request_type,
                          rs.name as request_status,
                          r.notes,
                          r.decline_reason
                        FROM
                          requests r
                          JOIN request_type rt ON r.request_type_id = rt.idRequestType
                          JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                        WHERE
                          rs.idRequestStatus = 2
                        """;
                break;
            case 3:
                query = """
                        SELECT
                          r.request_id,
                          r.request_name,
                          rt.name as request_type,
                          rs.name as request_status,
                          r.notes,
                          r.decline_reason
                        FROM
                          requests r
                          JOIN request_type rt ON r.request_type_id = rt.idRequestType
                          JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                        WHERE
                          r.user_id = @userId
                        """;
                break;
            case 4:
                query = """
                        SELECT
                          r.request_id,
                          r.request_name,
                          rt.name AS request_type,
                          rs.name AS request_status,
                          r.notes,
                          r.decline_reason
                        FROM
                          requests r
                          JOIN request_type rt ON r.request_type_id = rt.idRequestType
                          JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                        WHERE
                          r.request_type_id = (
                            SELECT
                              s.request_type_id
                            FROM
                              suppliers s
                              JOIN users u ON u.organization_id = s.supplier_id
                            WHERE
                              u.user_id = @userId
                          )
                          AND r.request_status_id = 4;
                        """;
                break;
            default:
                // Для неопределенных ролей возвращаем пустой список
                return requests;
        }

        await using var command = new MySqlCommand(query, connection);

        // Если необходимо, добавляем параметры к запросу
        if (user.RoleId == 3 || user.RoleId == 4) command.Parameters.AddWithValue("@userId", user.UserId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fileList = await GetRequestFilesAsync(reader.GetInt32("request_id"));
            var request = new Request
            {
                RequestId = reader.GetInt32("request_id"),
                RequestName = reader.GetString("request_name"),
                RequestType = reader.GetString("request_type"),
                RequestStatus = reader.GetString("request_status"),
                Notes = reader.GetString("notes"),
                DeclineReason = reader.IsDBNull(reader.GetOrdinal("decline_reason"))
                    ? null
                    : reader.GetString("decline_reason"), // Чтение decline_reason
                RequestFiles = new ObservableCollection<RequestFile>(fileList)
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

        const string query = """
                             INSERT INTO requests (
                               request_name, notes, user_id, request_status_id,
                               request_type_id
                             )
                             VALUES
                               (
                                 @RequestName, @Notes, @UserId, @RequestStatusId,
                                 @RequestTypeId
                               );
                             SELECT
                               LAST_INSERT_ID();
                             """;

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestName", request.RequestName);
        command.Parameters.AddWithValue("@Notes", request.Notes);
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@RequestStatusId", await GetRequestStatusIdAsync("В обработке"));
        command.Parameters.AddWithValue("@RequestTypeId", await GetRequestTypeIdAsync(request.RequestType));

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public static async Task AddRequestFilesAsync(int requestId, ObservableCollection<RequestFile> requestFiles)
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

    public static async Task<byte[]> GetFileDataByRequestIdAndFileNameAsync(int requestId, string fileName)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
            const string query =
                "SELECT file_data FROM request_files WHERE request_id = @RequestId AND file_name = @FileName";
            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@RequestId", requestId);
            command.Parameters.AddWithValue("@FileName", fileName);

            var result = await command.ExecuteScalarAsync();
            return result as byte[] ?? Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    public static async Task<bool> DeleteFileFromDatabaseAsync(int requestId, string fileName)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "DELETE FROM request_files WHERE request_id = @RequestId AND file_name = @FileName";
            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@RequestId", requestId);
            command.Parameters.AddWithValue("@FileName", fileName);

            // Execute the command and check if any rows were affected
            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows > 0; // Return true if one or more rows were affected
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> DeleteRequestWithFilesAsync(int requestId)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            const string deleteFilesQuery = "DELETE FROM request_files WHERE request_id = @RequestId";
            const string deleteRequestQuery = "DELETE FROM requests WHERE request_id = @RequestId";

            await ExecuteDeleteCommandAsync(deleteFilesQuery, requestId, connection, transaction);
            await ExecuteDeleteCommandAsync(deleteRequestQuery, requestId, connection, transaction);

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Ошибка: {ex.Message}");
            return false;
        }
    }

    private static async Task ExecuteDeleteCommandAsync(string query, int requestId, MySqlConnection connection,
        MySqlTransaction transaction)
    {
        await using var command = new MySqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@RequestId", requestId);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task UpdateRequestAsync(Request request)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        // Обновление запроса, чтобы включить изменение статуса
        const string query = """
                             UPDATE
                               requests
                             SET
                               request_name = @RequestName,
                               notes = @Notes,
                               request_status_id = IF(
                                 request_status_id = @DeclinedStatus,
                                 @InProcessStatus, request_status_id
                               )
                             WHERE
                               request_id = @RequestId
                             """;

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@RequestId", request.RequestId);
        command.Parameters.AddWithValue("@RequestName", request.RequestName);
        command.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);

        // Добавьте параметры для статусов
        command.Parameters.AddWithValue("@DeclinedStatus", 3); // Предполагая, что 4 - это "Отклонена"
        command.Parameters.AddWithValue("@InProcessStatus", 2); // Предполагая, что 2 - это "В обработке"

        await command.ExecuteNonQueryAsync();
    }

    public static async Task<bool> ChangeRequestStatus(int requestId, int newStatusId)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query = "UPDATE requests SET request_status_id = @NewStatus WHERE request_id = @RequestId";
        await using var command = new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@NewStatus", newStatusId);
        command.Parameters.AddWithValue("@RequestId", requestId);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public static async Task<bool> ChangeRequestStatusAndReason(int requestId, int newStatusId, string declineReason)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string query = """
                             UPDATE
                               requests
                             SET
                               request_status_id = @NewStatus,
                               decline_reason = @DeclineReason
                             WHERE
                               request_id = @RequestId
                             """;

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@NewStatus", newStatusId);
        command.Parameters.AddWithValue("@DeclineReason", declineReason);
        command.Parameters.AddWithValue("@RequestId", requestId);

        return await command.ExecuteNonQueryAsync() > 0;
    }
}