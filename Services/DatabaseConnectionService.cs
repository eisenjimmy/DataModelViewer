using Microsoft.Data.SqlClient;
using System.Data;

namespace SchemaDiagramViewer.Services;

public class DatabaseConnectionService
{
    public async Task<bool> TestConnectionAsync(string server, string? database, string? username, string? password, bool useWindowsAuth)
    {
        try
        {
            var connectionString = BuildConnectionString(server, database, username, password, useWindowsAuth);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetDatabasesAsync(string server, string? username, string? password, bool useWindowsAuth)
    {
        var databases = new List<string>();
        try
        {
            var connectionString = BuildConnectionString(server, null, username, password, useWindowsAuth);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb') ORDER BY name", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving databases: {ex.Message}", ex);
        }
        
        return databases;
    }

    private string BuildConnectionString(string server, string? database, string? username, string? password, bool useWindowsAuth)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database ?? "master",
            TrustServerCertificate = true
        };

        if (useWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = password;
        }

        return builder.ConnectionString;
    }

    public SqlConnection CreateConnection(string server, string database, string? username, string? password, bool useWindowsAuth)
    {
        var connectionString = BuildConnectionString(server, database, username, password, useWindowsAuth);
        return new SqlConnection(connectionString);
    }
}

