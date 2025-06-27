namespace ICTLU2_Backend_WebAPI.DTO
{
    public record WorldCreateDto(string Name, int Width, int Height);
    public record ObjectCreateDto(string Type, float X, float Y);
}
