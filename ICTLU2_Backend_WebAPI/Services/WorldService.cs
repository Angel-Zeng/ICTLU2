using Microsoft.Data.SqlClient;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;

namespace ICTLU2_Backend_WebAPI.Services;

//Heb ook WorldController zitten refractoren nadat Marc zei dat er teveel logica in zat. 
public class WorldService(ConnectionStrings conString) : IWorldService
{
    private readonly string _connectionString = conString.Sql;

    public async Task<List<World>> GetUserWorldsAsync(int userId)
    {
        var worlds = new List<World>();

        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT Id, Name, Width, Height FROM Worlds WHERE UserId = @uid",
            con);

        cmd.Parameters.AddWithValue("@uid", userId);
        using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
            worlds.Add(new World(
                r.GetInt32(0),
                r.GetString(1),
                r.GetInt32(2),
                r.GetInt32(3)));

        return worlds;
    }

    public async Task<int> CreateWorldAsync(int userId, WorldCreateDto dto)
    {
        // Validatie voor de wereld
        if (dto.Name.Length is < 1 or > 25)
            throw new ArgumentException("Naam moet tussen 1-25 tekens zijn");
        if (dto.Width is < 20 or > 200)
            throw new ArgumentException("Breedte moet tussen 20-200 zijn");
        if (dto.Height is < 10 or > 100)
            throw new ArgumentException("Hoogte moet tussen 10-100 zijn");

        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();
        using var tx = (SqlTransaction)await con.BeginTransactionAsync();

        try
        {
            // Kijken of de gebruiker al 5 werelden heeft
            var countCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Worlds WHERE UserId = @uid",
                con, tx);

            countCmd.Parameters.AddWithValue("@uid", userId);
            if ((int)await countCmd.ExecuteScalarAsync()! >= 5)
                throw new InvalidOperationException("Maximaal 5 werelden per gebruiker");

            //Kijken of de naam al bestaat in werelden
            var nameCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Worlds WHERE UserId = @uid AND Name = @name",
                con, tx);

            nameCmd.Parameters.AddWithValue("@uid", userId);
            nameCmd.Parameters.AddWithValue("@name", dto.Name);
            if ((int)await nameCmd.ExecuteScalarAsync()! > 0)
                throw new ArgumentException("Wereldnaam bestaat al");

            // Dit is zodat de wereld een unieke Id krijgt per gebruiker
            var idCmd = new SqlCommand(
                "SELECT ISNULL(MAX(Id), 0) + 1 FROM Worlds WHERE UserId = @uid",
                con, tx);

            idCmd.Parameters.AddWithValue("@uid", userId);
            var newId = (int)await idCmd.ExecuteScalarAsync();

            // Wereld aanmaken
            var insertCmd = new SqlCommand(
                @"INSERT INTO Worlds (UserId, Id, Name, Width, Height)
                VALUES (@uid, @id, @name, @width, @height);",
                con, tx);

            insertCmd.Parameters.AddWithValue("@uid", userId);
            insertCmd.Parameters.AddWithValue("@id", newId);
            insertCmd.Parameters.AddWithValue("@name", dto.Name);
            insertCmd.Parameters.AddWithValue("@width", dto.Width);
            insertCmd.Parameters.AddWithValue("@height", dto.Height);
            await insertCmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return newId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    //Opahlen van wereld met de objecten die erin zitten
    public async Task<(World World, List<WorldObject> Objects)> GetWorldAsync(int userId, int worldId)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();

        //Ophalen van de wereld uit de database
        var worldCmd = new SqlCommand(
            "SELECT Name, Width, Height FROM Worlds WHERE Id = @id AND UserId = @uid",
            con);

        worldCmd.Parameters.AddWithValue("@id", worldId);
        worldCmd.Parameters.AddWithValue("@uid", userId);

        using var r = await worldCmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
            throw new KeyNotFoundException("Wereld niet gevonden");

        var world = new World(
            worldId,
            r.GetString(0),
            r.GetInt32(1),
            r.GetInt32(2));

        await r.CloseAsync();

        // Objecten krijgen
        var objects = new List<WorldObject>();
        var objCmd = new SqlCommand(
            "SELECT Id, Type, X, Y FROM WorldObjects WHERE WorldId = @worldId",
            con);

        objCmd.Parameters.AddWithValue("@worldId", worldId);
        using var ro = await objCmd.ExecuteReaderAsync();

        while (await ro.ReadAsync())
            objects.Add(new WorldObject(
                ro.GetInt32(0),
                ro.GetString(1),
                (float)ro.GetDouble(2),
                (float)ro.GetDouble(3)));

        return (world, objects);
    }

    public async Task DeleteWorldAsync(int userId, int worldId)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();

        var cmd = new SqlCommand(
            "DELETE FROM Worlds WHERE Id = @id AND UserId = @uid",
            con);

        cmd.Parameters.AddWithValue("@id", worldId);
        cmd.Parameters.AddWithValue("@uid", userId);

        int affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0)
            throw new KeyNotFoundException("Wereld niet gevonden");
    }

    public async Task<int> AddObjectToWorldAsync(int userId, int worldId, ObjectCreateDto dto)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();

        // Wereld dimensies ophalen
        var boundsCmd = new SqlCommand(
            "SELECT Width, Height FROM Worlds WHERE UserId = @uid AND Id = @id",
            con);

        boundsCmd.Parameters.AddWithValue("@uid", userId);
        boundsCmd.Parameters.AddWithValue("@id", worldId);

        using var r = await boundsCmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
            throw new KeyNotFoundException("Wereld niet gevonden");

        int width = r.GetInt32(0);
        int height = r.GetInt32(1);
        await r.CloseAsync();

        // de positie van het object checken
        if (dto.X > width || dto.Y > height)
            throw new ArgumentException("Object valt buiten wereldgrenzen");

        // En dan object toevoegen 
        var insertCmd = new SqlCommand(
            "INSERT INTO WorldObjects (UserId, WorldId, Type, X, Y) " +
            "OUTPUT INSERTED.Id " +
            "VALUES (@uid, @worldId, @type, @x, @y);",
            con);

        insertCmd.Parameters.AddWithValue("@uid", userId);
        insertCmd.Parameters.AddWithValue("@worldId", worldId);
        insertCmd.Parameters.AddWithValue("@type", dto.Type);
        insertCmd.Parameters.AddWithValue("@x", dto.X);
        insertCmd.Parameters.AddWithValue("@y", dto.Y);

        return (int)await insertCmd.ExecuteScalarAsync();
    }
}