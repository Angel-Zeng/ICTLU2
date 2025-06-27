using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using ICTLU2_Backend_WebAPI.Models;
using System.Security.Claims;
using ICTLU2_Backend_WebAPI.DTO;

namespace ICTLU2_Backend_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorldsController : ControllerBase
{
    private readonly ConnectionStrings _cs;
    public WorldsController(ConnectionStrings cs) { _cs = cs; }

    // Gets the UserId from the JWT token (in AutController.cs) and gets user's data
    int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    // Retrieves all worlds for the current user
    public IActionResult MyWorlds()
    {
        var list = new List<World>();
        using var con = new SqliteConnection(_cs.DefaultConnection);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Width, Height FROM Worlds WHERE UserId = @uid";
        cmd.Parameters.AddWithValue("@uid", UserId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new World(r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3)));
        // "id": 1, "name": "MyBeautifulWorld", "width": 50, "height": 30 },
        return Ok(list);
    }

    [HttpGet("{id}")]
    // Verifying that the world belongs to this user, loads the world, queries all the objects: 
    //"world": { "id": 1, "name": "MyBeautifulWorld, "width": 50, "height": 30 },
    //"objects": { "id": 10, "type": "red_Pikmin", "x": 12.5, "y": 6.2 },
    public IActionResult GetWorld(int id)
    {
        using var con = new SqliteConnection(_cs.DefaultConnection);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Name, Width, Height FROM Worlds WHERE Id = @id AND UserId = @uid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", UserId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return NotFound();
        var world = new World(id, r.GetString(0), r.GetInt32(1), r.GetInt32(2));

        // Get objects
        var objects = new List<WorldObject>();
        using var cmd2 = con.CreateCommand();
        cmd2.CommandText = "SELECT Id, Type, X, Y FROM WorldObjects WHERE WorldId = @wid";
        cmd2.Parameters.AddWithValue("@wid", id);
        using var ro = cmd2.ExecuteReader();
        while (ro.Read())
            objects.Add(new WorldObject(ro.GetInt32(0), ro.GetString(1), (float)ro.GetDouble(2), (float)ro.GetDouble(3)));
        return Ok(new { world, objects });
    }

    [HttpPost]
    //When making a new world
    // World name has to be between 1 and 25 chars, width between 20 and 200, height between 10 and 100. 
    public IActionResult Create([FromBody] WorldCreateDto dto)
    {
        if (dto.Name.Length is < 1 or > 25) return BadRequest("Name length invalid");
        if (dto.Width is < 20 or > 200) return BadRequest("Width out of range");
        if (dto.Height is < 10 or > 100) return BadRequest("Height out of range");

        using var con = new SqliteConnection(_cs.DefaultConnection);
        con.Open();
        // Checking if the user already has 5 worlds or if the world name already exists. 
        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT (SELECT COUNT(*) FROM Worlds WHERE UserId=@uid) AS cnt, (SELECT COUNT(*) FROM Worlds WHERE UserId=@uid AND Name=@n) AS dup";
            check.Parameters.AddWithValue("@uid", UserId);
            check.Parameters.AddWithValue("@n", dto.Name);
            using var r = check.ExecuteReader();
            r.Read();
            if (r.GetInt64(0) >= 5) return BadRequest("Max 5 worlds");
            if (r.GetInt64(1) > 0) return BadRequest("Name exists");
        }
        // Accepts world creation if all okay and inserts it into Worlds table 
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO Worlds (Name, Width, Height, UserId) VALUES (@n, @w, @h, @uid); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@n", dto.Name);
        cmd.Parameters.AddWithValue("@w", dto.Width);
        cmd.Parameters.AddWithValue("@h", dto.Height);
        cmd.Parameters.AddWithValue("@uid", UserId);
        long newId = (long)cmd.ExecuteScalar()!;
        return CreatedAtAction(nameof(GetWorld), new { id = newId }, new { id = newId });
    }

    // Deleting a world if it exists and belongs to the current user. 

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        using var con = new SqliteConnection(_cs.DefaultConnection);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM Worlds WHERE Id=@id AND UserId=@uid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", UserId);
        int affected = cmd.ExecuteNonQuery();
        return affected == 0 ? NotFound() : Ok("World deletected succesfully!");
    }

    [HttpPost("{id}/objects")]
    // Checks if the world exists and belongs to the user, then checks if the object is within bounds of the world. 
    public IActionResult AddObject(int id, [FromBody] ObjectCreateDto dto)
    {
        using var con = new SqliteConnection(_cs.DefaultConnection);
        con.Open();
        // Verify world and bounds
        using (var worldCmd = con.CreateCommand())
        {
            worldCmd.CommandText = "SELECT Width, Height FROM Worlds WHERE Id=@id AND UserId=@uid";
            worldCmd.Parameters.AddWithValue("@id", id);
            worldCmd.Parameters.AddWithValue("@uid", UserId);
            using var r = worldCmd.ExecuteReader();
            if (!r.Read()) return NotFound();
            int w = r.GetInt32(0), h = r.GetInt32(1);
            if (dto.X > w || dto.Y > h) return BadRequest("Object outside bounds");
        }
        // Insert
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO WorldObjects (Type, X, Y, WorldId) VALUES (@t, @x, @y, @wid); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@t", dto.Type);
        cmd.Parameters.AddWithValue("@x", dto.X);
        cmd.Parameters.AddWithValue("@y", dto.Y);
        cmd.Parameters.AddWithValue("@wid", id);
        long objId = (long)cmd.ExecuteScalar()!;
        return Ok(new { objId });
    }
}