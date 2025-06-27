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
public class WorldsController(ConnectionStrings cs) : ControllerBase
{
    readonly ConnectionStrings _cs = cs;

    int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ───── GET api/worlds ──────────────────────────────────────
    [HttpGet]
    public IActionResult MyWorlds()
    {
        var list = new List<World>();
        using var con = new SqlConnection(_cs.Sql); con.Open();
        using var cmd = new SqlCommand("SELECT Id,Name,Width,Height FROM Worlds WHERE UserId=@uid", con);
        cmd.Parameters.AddWithValue("@uid", UserId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new World(r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3)));
        return Ok(list);
    }

    // ───── POST api/worlds ─────────────────────────────────────
    [HttpPost]
    public IActionResult Create(WorldCreateDto dto)
    {
        if (dto.Name.Length is < 1 or > 25) return BadRequest("Name length invalid");
        if (dto.Width is < 20 or > 200) return BadRequest("Width out of range");
        if (dto.Height is < 10 or > 100) return BadRequest("Height out of range");

        using var con = new SqlConnection(_cs.Sql); con.Open();

        // 1️⃣ max 5 worlds per user + unique name
        using (var chk = new SqlCommand("SELECT COUNT(*) FROM Worlds WHERE UserId=@uid", con))
        {
            chk.Parameters.AddWithValue("@uid", UserId);
            if ((int)chk.ExecuteScalar()! >= 5) return BadRequest("Max 5 worlds reached");
        }
        using (var dup = new SqlCommand("SELECT COUNT(*) FROM Worlds WHERE UserId=@uid AND Name=@n", con))
        {
            dup.Parameters.AddWithValue("@uid", UserId);
            dup.Parameters.AddWithValue("@n", dto.Name);
            if ((int)dup.ExecuteScalar()! > 0) return BadRequest("Name already in use");
        }

        // 2️⃣ insert & return identity Id
        int worldId;
        using (var ins = new SqlCommand(@"INSERT INTO Worlds (Name,Width,Height,UserId)
                                          VALUES (@n,@w,@h,@uid); SELECT SCOPE_IDENTITY();", con))
        {
            ins.Parameters.AddWithValue("@n", dto.Name);
            ins.Parameters.AddWithValue("@w", dto.Width);
            ins.Parameters.AddWithValue("@h", dto.Height);
            ins.Parameters.AddWithValue("@uid", UserId);
            worldId = Convert.ToInt32(ins.ExecuteScalar());
        }

        return CreatedAtAction(nameof(GetWorld), new { id = worldId }, new { id = worldId });
    }

    // ───── GET api/worlds/{id} ─────────────────────────────────
    [HttpGet("{id}")]
    public IActionResult GetWorld(int id)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();

        string name; int width, height;
        using (var cmd = new SqlCommand("SELECT Name,Width,Height FROM Worlds WHERE Id=@id AND UserId=@uid", con))
        {
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
        using (var oCmd = new SqlCommand("SELECT Id,Type,X,Y FROM WorldObjects WHERE WorldId=@wid", con))
        {
            oCmd.Parameters.AddWithValue("@wid", id);
            using var ro = oCmd.ExecuteReader();
            while (ro.Read())
                objects.Add(new WorldObject(ro.GetInt32(0), ro.GetString(1), (float)ro.GetDouble(2), (float)ro.GetDouble(3)));
        }
        return Ok(new { world, objects });
    }

    // ───── DELETE api/worlds/{id} ──────────────────────────────
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();
        using var cmd = new SqlCommand("DELETE FROM Worlds WHERE Id=@id AND UserId=@uid", con);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@uid", UserId);
        return cmd.ExecuteNonQuery() == 0 ? NotFound() : Ok("Succesfully deleted!");
    }

    // ───── POST api/worlds/{id}/objects ────────────────────────
    [HttpPost("{id}/objects")]
    public IActionResult AddObject(int id, ObjectCreateDto dto)
    {
        using var con = new SqlConnection(_cs.Sql); con.Open();

        // verify world ownership & bounds
        using (var wCmd = new SqlCommand("SELECT Width,Height FROM Worlds WHERE Id=@id AND UserId=@uid", con))
        {
            wCmd.Parameters.AddWithValue("@id", id);
            wCmd.Parameters.AddWithValue("@uid", UserId);
            using var r = wCmd.ExecuteReader();
            if (!r.Read()) return NotFound();
            if (dto.X > r.GetInt32(0) || dto.Y > r.GetInt32(1)) return BadRequest("Object outside bounds");
        }

        // insert object
        int objId;
        using (var cmd = new SqlCommand("INSERT INTO WorldObjects (Type,X,Y,WorldId) VALUES (@t,@x,@y,@wid); SELECT SCOPE_IDENTITY();", con))
        {
            cmd.Parameters.AddWithValue("@t", dto.Type);
            cmd.Parameters.AddWithValue("@x", dto.X);
            cmd.Parameters.AddWithValue("@y", dto.Y);
            cmd.Parameters.AddWithValue("@wid", id);
            objId = Convert.ToInt32(cmd.ExecuteScalar());
        }
        return Ok(new { objId });
    }
}