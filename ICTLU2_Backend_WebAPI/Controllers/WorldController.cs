using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Services;
using System.Security.Claims;

namespace ICTLU2_Backend_WebAPI.Controllers;

// Alle endpoints die te maken hebben met werelden van een user. Werken alleen als de user ingelogd is. 

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorldsController(IWorldService worldService) : ControllerBase
{
    private readonly IWorldService _worldService = worldService;

    //Haalt user id uit de token, dit zorgrt er dus voor dat alles alleen werkt als je ingelogd bent. 
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    //Ophalen van de werelden van een user
    [HttpGet]
    public async Task<IActionResult> GetUserWorlds()
    {
        return Ok(await _worldService.GetUserWorldsAsync(UserId));
    }

    //Het maken van werelden
    [HttpPost]
    public async Task<IActionResult> CreateWorld(WorldCreateDto dto)
    {
        try
        {
            var worldId = await _worldService.CreateWorldAsync(UserId, dto);
            return CreatedAtAction(
                nameof(GetWorld),
                new { id = worldId },
                new { id = worldId });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    //haalt specifieke werelden op met objecten
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorld(int id)
    {
        try
        {
            var (world, objects) = await _worldService.GetWorldAsync(UserId, id);
            return Ok(new { world, objects });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    //verwijdert werelden op basis van wereldid

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorld(int id)
    {
        try
        {
            await _worldService.DeleteWorldAsync(UserId, id);
            return Ok("Wereld succesvol verwijderd");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    //toevoegen van objecten aan een wereld
    [HttpPost("{id}/objects")]
    public async Task<IActionResult> AddObjectToWorld(int id, ObjectCreateDto dto)
    {
        try
        {
            var objectId = await _worldService.AddObjectToWorldAsync(UserId, id, dto);
            return Ok(new { objectId });
        }
        catch (Exception ex)
        {
            return ex is KeyNotFoundException ? NotFound(ex.Message) : BadRequest(ex.Message);
        }
    }
}