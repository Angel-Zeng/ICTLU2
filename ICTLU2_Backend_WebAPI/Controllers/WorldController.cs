using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;
using System.Security.Claims;

namespace ICTLU2_Backend_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorldsController(ConnectionStrings conString) : ControllerBase
{
    private readonly ConnectionStrings _conString = conString;

   //Haalt de gebruiker id
    private int UserId => int.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    //Werelden van de gebruiker ophalen 
    [HttpGet]
    public IActionResult GetUserWorlds()
    {
        var worlds = new List<World>();

        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        using var cmd = new SqlCommand(
            "SELECT Id, Name, Width, Height " +
            "FROM Worlds WHERE UserId = @uid",
            con);

        cmd.Parameters.AddWithValue("@uid", UserId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            worlds.Add(new World(
                r.GetInt32(0),
                r.GetString(1),
                r.GetInt32(2),
                r.GetInt32(3)));

        return Ok(worlds);
    }

    //Maken van nieuwe werelden
    [HttpPost]
    public IActionResult CreateWorld(WorldCreateDto dto)
    {
        // Valideer invoer
        if (dto.Name.Length is < 1 or > 25)
            return BadRequest("Naam moet tussen 1-25 tekens zijn");

        if (dto.Width is < 20 or > 200)
            return BadRequest("Breedte moet tussen 20-200 zijn");

        if (dto.Height is < 10 or > 100)
            return BadRequest("Hoogte moet tussen 10-100 zijn");

        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        //starten van een transactie omdat we meerdere queries uitvoeren
        using var tx = con.BeginTransaction();

        try
        {
            //Max 5 werelden per gebruiker
            using var countCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Worlds WHERE UserId = @uid",
                con, tx);

            countCmd.Parameters.AddWithValue("@uid", UserId);
            if ((int)countCmd.ExecuteScalar()! >= 5)
                return BadRequest("Maximaal 5 werelden per gebruiker");

            //Mag niet dezelfde naam gebruiken
            using var nameCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Worlds " +
                "WHERE UserId = @uid AND Name = @name",
                con, tx);

            nameCmd.Parameters.AddWithValue("@uid", UserId);
            nameCmd.Parameters.AddWithValue("@name", dto.Name);

            if ((int)nameCmd.ExecuteScalar()! > 0)
                return BadRequest("Wereldnaam bestaat al");

            //Elke wereld een unieke id geven
            int newId;
            using var idCmd = new SqlCommand(
                "SELECT ISNULL(MAX(Id), 0) + 1 " +
                "FROM Worlds WHERE UserId = @uid",
                con, tx);

            idCmd.Parameters.AddWithValue("@uid", UserId);
            newId = (int)idCmd.ExecuteScalar()!;

            //De wereld toevoegen aan de database
            using var insertCmd = new SqlCommand(
                @"INSERT INTO Worlds (UserId, Id, Name, Width, Height)
                VALUES (@uid, @id, @name, @width, @height);",
                con, tx);

            insertCmd.Parameters.AddWithValue("@uid", UserId);
            insertCmd.Parameters.AddWithValue("@id", newId);
            insertCmd.Parameters.AddWithValue("@name", dto.Name);
            insertCmd.Parameters.AddWithValue("@width", dto.Width);
            insertCmd.Parameters.AddWithValue("@height", dto.Height);
            insertCmd.ExecuteNonQuery();

            tx.Commit();
            return CreatedAtAction(
                nameof(GetWorld),
                new { id = newId },
                new { id = newId });
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    //Ophalen van een specifieke wereld
    [HttpGet("{id}")]
    public IActionResult GetWorld(int id)
    {
        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        //Ophalen van de wereldgegevens
        World world;
        using (var worldCmd = new SqlCommand(
            "SELECT Name, Width, Height " +
            "FROM Worlds WHERE Id = @id AND UserId = @uid",
            con))
        {
            worldCmd.Parameters.AddWithValue("@id", id);
            worldCmd.Parameters.AddWithValue("@uid", UserId);

            using var r = worldCmd.ExecuteReader();
            if (!r.Read())
                return NotFound("Wereld niet gevonden");

            world = new World(
                id,
                r.GetString(0),
                r.GetInt32(1),
                r.GetInt32(2));
        }

        //ophalen van de objecten die in de wereld zitten
        var objects = new List<WorldObject>();
        using (var objCmd = new SqlCommand(
            "SELECT Id, Type, X, Y " +
            "FROM WorldObjects WHERE WorldId = @worldId",
            con))
        {
            objCmd.Parameters.AddWithValue("@worldId", id);

            using var ro = objCmd.ExecuteReader();
            while (ro.Read())
                objects.Add(new WorldObject(
                    ro.GetInt32(0),
                    ro.GetString(1),
                    (float)ro.GetDouble(2),
                    (float)ro.GetDouble(3)));
        }

        return Ok(new { world, objects });
    }

    //Verwijderen!
    [HttpDelete("{id}")]
    public IActionResult DeleteWorld(int id)
    {
        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        using var cmd = new SqlCommand(
            "DELETE FROM Worlds " +
            "WHERE Id = @id AND UserId = @uid",
            con);

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", UserId);

        int affected = cmd.ExecuteNonQuery();
        return affected == 0
            ? NotFound("Wereld niet gevonden")
            : Ok("Wereld succesvol verwijderd");
    }

    //objecten toevoegen aan wereld 
    [HttpPost("{id}/objects")]
    public IActionResult AddObjectToWorld(int id, ObjectCreateDto dto)
    {
        using var con = new SqlConnection(_conString.Sql);
        con.Open();

        //De wereld grenzen ophalen
        int width, height;
        using (var boundsCmd = new SqlCommand(
            "SELECT Width, Height FROM Worlds " +
            "WHERE UserId = @uid AND Id = @id",
            con))
        {
            boundsCmd.Parameters.AddWithValue("@uid", UserId);
            boundsCmd.Parameters.AddWithValue("@id", id);

            using var r = boundsCmd.ExecuteReader();
            if (!r.Read())
                return NotFound("Wereld niet gevonden");

            width = r.GetInt32(0);
            height = r.GetInt32(1);
        }

        //valideren van object invoer of die binnen de wereld past
        if (dto.X > width || dto.Y > height)
            return BadRequest("Object valt buiten wereldgrenzen");

        //objectentoevoegen 
        int newObjId;
        using (var insertCmd = new SqlCommand(
            "INSERT INTO WorldObjects (UserId, WorldId, Type, X, Y) " +
            "VALUES (@uid, @worldId, @type, @x, @y); " +
            "SELECT SCOPE_IDENTITY();",
            con))
        {
            insertCmd.Parameters.AddWithValue("@uid", UserId);
            insertCmd.Parameters.AddWithValue("@worldId", id);
            insertCmd.Parameters.AddWithValue("@type", dto.Type);
            insertCmd.Parameters.AddWithValue("@x", dto.X);
            insertCmd.Parameters.AddWithValue("@y", dto.Y);

            newObjId = Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        return Ok(new { objectId = newObjId });
    }
}