using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public class UserChecker : IUserChecker
{
    private readonly string _connectionString;

    public UserChecker(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<bool> UserExistsAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var guid)) return false;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            """SELECT COUNT(1) FROM "Users" WHERE "Id" = @id""", conn);
        cmd.Parameters.AddWithValue("id", guid);

        var count = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(count) > 0;
    }
}
