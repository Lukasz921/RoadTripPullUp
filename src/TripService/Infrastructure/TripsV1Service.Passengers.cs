using Application.Exceptions;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsV1Service
{
    public async Task AddPassengerAsync(string tripId, string driverId, string passengerId)
    {
        if (!Guid.TryParse(tripId, out var tripGuid))
            throw new NotFoundException($"Trip '{tripId}' not found.");
        if (!Guid.TryParse(driverId, out var driverGuid))
            throw new ForbiddenException("Invalid driver identity.");
        if (!Guid.TryParse(passengerId, out var passengerGuid))
            throw new ValidationException("passengerId is not a valid UUID.");
        if (driverGuid == passengerGuid)
            throw new ValidationException("Driver cannot be added as a passenger on their own trip.");

        if (!await _userChecker.UserExistsAsync(passengerId))
            throw new ValidationException($"User '{passengerId}' does not exist.");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        const string checkSql = """
            SELECT
                t.driver_user_id,
                t.status::text,
                t.available_seats,
                (SELECT COUNT(*) FROM trip_passenger WHERE trip_id = t.id) AS passenger_count,
                EXISTS(SELECT 1 FROM trip_passenger WHERE trip_id = t.id AND passenger_user_id = @passengerId) AS already_joined
            FROM trip t
            WHERE t.id = @id
            FOR UPDATE
            """;

        await using var checkCmd = new NpgsqlCommand(checkSql, conn, tx);
        checkCmd.Parameters.AddWithValue("id",          tripGuid);
        checkCmd.Parameters.AddWithValue("passengerId", passengerGuid);

        await using var r = await checkCmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
        {
            await tx.RollbackAsync();
            throw new NotFoundException($"Trip '{tripId}' not found.");
        }

        var tripDriverId   = r.GetGuid(r.GetOrdinal("driver_user_id"));
        var status         = r.GetString(r.GetOrdinal("status"));
        var availableSeats = r.GetInt16(r.GetOrdinal("available_seats"));
        var passengerCount = r.GetInt64(r.GetOrdinal("passenger_count"));
        var alreadyJoined  = r.GetBoolean(r.GetOrdinal("already_joined"));
        await r.CloseAsync();

        if (tripDriverId != driverGuid)
            throw new ForbiddenException("Only the trip driver can add passengers.");
        if (status != "ACTIVE")
            throw new ValidationException("Trip is not active.");
        if (alreadyJoined)
            throw new ValidationException("This user is already a passenger on this trip.");
        if (passengerCount >= availableSeats)
            throw new SeatUnavailableException("No seats available on this trip.");

        await using var insertCmd = new NpgsqlCommand(
            "INSERT INTO trip_passenger (trip_id, passenger_user_id) VALUES (@id, @passengerId)",
            conn, tx);
        insertCmd.Parameters.AddWithValue("id",          tripGuid);
        insertCmd.Parameters.AddWithValue("passengerId", passengerGuid);
        await insertCmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
    }
}
