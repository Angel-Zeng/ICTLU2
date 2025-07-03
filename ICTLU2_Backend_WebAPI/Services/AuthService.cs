using Microsoft.Data.SqlClient;
using ICTLU2_Backend_WebAPI.DTO;
using ICTLU2_Backend_WebAPI.Models;

namespace ICTLU2_Backend_WebAPI.Services;

// Heb deze toegevoegd omdat Marc vond dat er te veel code in the Controllers stonden. 
public class AuthService(ConnectionStrings conString) : IAuthService
{
    private readonly string _connectionString = conString.Sql;

    public async Task<string> RegisterUserAsync(LoginDto dto)
    {
        if (!ValidatePassword(dto.Password))
            throw new ArgumentException("Wachtwoord moet minimaal 10 tekens, 1 hoofdletters, 1 cijfers en 1 speciaal teken!");

        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();

        // Kijken of de gebruiker al bestaat 
        var checkCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE Username=@username", con);
        checkCmd.Parameters.AddWithValue("@username", dto.Username);
        if ((int)await checkCmd.ExecuteScalarAsync()! > 0)
            throw new ArgumentException("Gebruikersnaam is al in gebruik");

        // Maken van een nieuwe gebruiker
        var createCmd = new SqlCommand(
            "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (@username, @password);",
            con);

        createCmd.Parameters.AddWithValue("@username", dto.Username);
        createCmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(dto.Password));
        await createCmd.ExecuteNonQueryAsync();

        return "Registratie succesvol";
    }

    //Inloggen van een gebruiker
    public async Task<string> LoginUserAsync(LoginDto dto)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT Id, PasswordHash FROM dbo.Users WHERE Username=@u",
            con);

        cmd.Parameters.AddWithValue("@u", dto.Username);
        using var r = await cmd.ExecuteReaderAsync();

        if (!await r.ReadAsync())
            throw new UnauthorizedAccessException("Ongeldige gebruikersnaam of wachtwoord");

        var id = r.GetInt32(0);
        var hash = r.GetString(1);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, hash))
            throw new UnauthorizedAccessException("Ongeldige gebruikersnaam of wachtwoord");

        return id.ToString();
    }

    //Kijken of het wachtwoord wel aan de eisen voldoet. 
    private static bool ValidatePassword(string password) =>
        password.Length >= 10 &&
        password.Any(char.IsLower) &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsDigit) &&
        password.Any(ch => !char.IsLetterOrDigit(ch));
}