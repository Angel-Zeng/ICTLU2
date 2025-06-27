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
public class WorldsController : ControllerBase
{
    private readonly ConnectionStrings _cs;
    public WorldsController(ConnectionStrings cs) => _cs = cs;
    int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ─────────────────────────────────────────────────────────────
    //  GET /api/worlds           → worlds owned by current user
    // ─────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult MyWorlds()
    {
        var list = new List<World>();
        using var con = new SqlConnection(_cs.Sql); con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id,WorldNumber,Name,Width,Height FROM Worlds WHERE UserId=@uid";
        cmd.Parameters.AddWithValue("@uid", UserId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new World(r.GetInt32(0), r.GetString(2), r.GetInt32(3), r.GetInt32(4)));
        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────────
    //  POST /api/worlds          → create world, per‑user numbering
    // ─────────────────────────────────────────────────────────────
    [HttpPost]
    public IActionResult Create(WorldCreateDto dto)
    {
        if (dto.Name.Length is < 1 or > 25) return BadRequest("Name length invalid");
        if (dto.Width is < 20 or > 200) return BadRequest("Width out of range");
        if (dto.Height is < 10 or > 100) return BadRequest("Height out of range");

        using var con = new SqlConnection(_cs.Sql); con.Open();

        // 1️⃣ validation: max 5 worlds & unique name per user
        using (var chk = con.CreateCommand())
        {
            chk.CommandText = "SELECT COUNT(*) FROM Worlds WHERE UserId=@uid";
            chk.Parameters.AddWithValue("@uid", UserId);
            if ((int)chk.ExecuteScalar()! >= 5) return BadRequest("Max 5 worlds");
        }
        using (var dup = con.CreateCommand())
        {
            dup.CommandText = "SELECT COUNT(*) FROM Worlds WHERE UserId=@uid AND Name=@n";
            dup.Parameters.AddWithValue("@uid", UserId);
            dup.Parameters.AddWithValue("@n", dto.Name);
            if ((int)dup.ExecuteScalar()! > 0) return BadRequest("Name exists");
        }

        // 2️⃣ calculate next WorldNumber per user
        int worldNumber;
        using (var next = con.CreateCommand())
        {
            next.CommandText = "SELECT ISNULL(MAX(WorldNumber),0) + 1 FROM Worlds WHERE UserId=@uid";
            next.Parameters.AddWithValue("@uid", UserId);
            worldNumber = (int)next.ExecuteScalar()!;
        }

        // 3️⃣ insert and return both Id and WorldNumber
        int worldId;
        using (var ins = con.CreateCommand())
        {
            ins.CommandText = @"INSERT INTO Worlds (Name,Width,Height,UserId,WorldNumber)
                               VALUES (@n,@w,@h,@uid,@wn); SELECT SCOPE_IDENTITY();";
            ins.Parameters.AddWithValue("@n", dto.Name);
            ins.Parameters.AddWithValue("@w", dto.Width);
            ins.Parameters.AddWithValue("@h", dto.Height);
            ins.Parameters.AddWithValue("@uid", UserId);
            ins.Parameters.AddWithValue("@wn", worldNumber);
            worldId = Convert.ToInt32(ins.ExecuteScalar());
        }
        return CreatedAtAction(nameof(GetWorld), new { id = worldId }, new { id = worldId, worldNumber });
    }

    // ─────────────────────────────────────────────────────────────
    //  GET /api/worlds/{id}
    // ─────────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public IActionResult GetWorld(int id)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();
        string name; int width, height;
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "SELECT Name,Width,Height FROM Worlds WHERE Id=@id AND UserId=@uid";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@uid", UserId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return NotFound();
            name = r.GetString(0);
            width = r.GetInt32(1);
            height = r.GetInt32(2);
        }
        var world = new World(id, name, width, height);

        var objects = new List<WorldObject>();
        using (var oCmd = con.CreateCommand())
        {
            oCmd.CommandText = "SELECT Id,Type,X,Y FROM WorldObjects WHERE WorldId=@wid";
            oCmd.Parameters.AddWithValue("@wid", id);
            using var ro = oCmd.ExecuteReader();
            while (ro.Read())
                objects.Add(new WorldObject(ro.GetInt32(0), ro.GetString(1), (float)ro.GetDouble(2), (float)ro.GetDouble(3)));
        }
        return Ok(new { world, objects });
    }

    // ─────────────────────────────────────────────────────────────
    //  DELETE /api/worlds/{id}
    // ─────────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM Worlds WHERE Id=@id AND UserId=@uid";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", UserId);
        return cmd.ExecuteNonQuery() == 0 ? NotFound() : Ok();
    }

    // ─────────────────────────────────────────────────────────────
    //  POST /api/worlds/{id}/objects
    // ─────────────────────────────────────────────────────────────
    [HttpPost("{id}/objects")]
    public IActionResult AddObject(int id, ObjectCreateDto dto)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();
        using (var wCmd = con.CreateCommand())
        {
            wCmd.CommandText = "SELECT Width,Height FROM Worlds WHERE Id=@id AND UserId=@uid";
            wCmd.Parameters.AddWithValue("@id", id);
            wCmd.Parameters.AddWithValue("@uid", UserId);
            using var r = wCmd.ExecuteReader();
            if (!r.Read()) return NotFound();
            if (dto.X > r.GetInt32(0) || dto.Y > r.GetInt32(1)) return BadRequest("Object outside bounds");
        }
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO WorldObjects (Type,X,Y,WorldId) VALUES (@t,@x,@y,@wid); SELECT SCOPE_IDENTITY();";
        cmd.Parameters.AddWithValue("@t", dto.Type);
        cmd.Parameters.AddWithValue("@x", dto.X);
        cmd.Parameters.AddWithValue("@y", dto.Y);
        cmd.Parameters.AddWithValue("@wid", id);
        int objId = Convert.ToInt32(cmd.ExecuteScalar());
        return Ok(new { objId });
    }
}