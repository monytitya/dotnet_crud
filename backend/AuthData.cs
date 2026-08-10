using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;

interface IUserStore
{
    Task CreateAsync(UserRecord user);
    Task<UserRecord?> FindByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
}

sealed class OracleUserStore : IUserStore
{
    private readonly string _connectionString;

    public OracleUserStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task CreateAsync(UserRecord user)
    {
        const string sql = @"INSERT INTO APP_USERS (ID, EMAIL, PASSWORD_HASH, ROLE, IS_ACTIVE, DATE_CREATED, DATE_MODIFIED)
VALUES (:id, :email, :passwordHash, :role, :isActive, :dateCreated, :dateModified)";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new OracleCommand(sql, connection);
        command.Parameters.Add(new OracleParameter("id", user.Id));
        command.Parameters.Add(new OracleParameter("email", user.Email));
        command.Parameters.Add(new OracleParameter("passwordHash", user.PasswordHash));
        command.Parameters.Add(new OracleParameter("role", user.Role));
        command.Parameters.Add(new OracleParameter("isActive", user.IsActive ? 1 : 0));
        command.Parameters.Add(new OracleParameter("dateCreated", user.DateCreated));
        command.Parameters.Add(new OracleParameter("dateModified", user.DateModified));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<UserRecord?> FindByEmailAsync(string email)
    {
        const string sql = @"SELECT ID, EMAIL, PASSWORD_HASH, ROLE, IS_ACTIVE, DATE_CREATED, DATE_MODIFIED
FROM APP_USERS
WHERE EMAIL = :email";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new OracleCommand(sql, connection);
        command.Parameters.Add(new OracleParameter("email", email));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserRecord
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Role = reader.IsDBNull(3) ? "User" : reader.GetString(3),
            IsActive = reader.GetInt32(4) == 1,
            DateCreated = reader.GetDateTime(5),
            DateModified = reader.GetDateTime(6),
        };
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        const string sql = @"SELECT COUNT(1) FROM APP_USERS WHERE EMAIL = :email";

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new OracleCommand(sql, connection);
        command.Parameters.Add(new OracleParameter("email", email));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
}

class InMemoryUserStore : IUserStore
{
    private readonly Dictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);

    public Task CreateAsync(UserRecord user)
    {
        _users[user.Email] = user;
        return Task.CompletedTask;
    }

    public Task<UserRecord?> FindByEmailAsync(string email)
    {
        _users.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        return Task.FromResult(_users.ContainsKey(email));
    }
}

record UserRecord
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string PasswordHash { get; init; } = default!;
    public string Role { get; init; } = "User";
    public bool IsActive { get; init; } = true;
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;
    public DateTime DateModified { get; init; } = DateTime.UtcNow;
}

record RegisterRequest(string Email, string Password, string? Role);
record LoginRequest(string Email, string Password);
record AuthResponse(string Token, int ExpiresIn, Guid UserId, string Email, string Role);
