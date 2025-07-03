using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;

namespace ICTLU2_Backend_WebAPI.Services;

public interface IWorldService
{
    Task<List<World>> GetUserWorldsAsync(int userId);
    Task<int> CreateWorldAsync(int userId, WorldCreateDto dto);
    Task<(World World, List<WorldObject> Objects)> GetWorldAsync(int userId, int worldId);
    Task DeleteWorldAsync(int userId, int worldId);
    Task<int> AddObjectToWorldAsync(int userId, int worldId, ObjectCreateDto dto);
}