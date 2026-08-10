using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token with Bearer prefix, e.g. 'Bearer {token}'",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = GetJwtSecretKey(builder.Configuration);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "dotnet_angular",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "dotnet_angular_audience",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IUserStore>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Oracle connection string is required. Set ConnectionStrings:DefaultConnection in appsettings.json.");
    }

    return new OracleUserStore(connectionString);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (RegisterRequest request, IUserStore userStore) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
    if (await userStore.ExistsByEmailAsync(normalizedEmail))
    {
        return Results.Conflict(new { error = "A user with this email already exists." });
    }

    var user = new UserRecord
    {
        Id = Guid.NewGuid(),
        Email = normalizedEmail,
        PasswordHash = HashPassword(request.Password),
        Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim(),
    };

    await userStore.CreateAsync(user);
    return Results.Created($"/api/auth/users/{user.Id}", new { user.Id, user.Email, user.Role });
});

app.MapPost("/api/auth/login", async (LoginRequest request, IUserStore userStore) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
    var user = await userStore.FindByEmailAsync(normalizedEmail);
    if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var token = BuildJwtToken(user, builder.Configuration);
    return Results.Ok(new AuthResponse(token, 3600, user.Id, user.Email, user.Role));
});

app.MapGet("/api/auth/profile", [Microsoft.AspNetCore.Authorization.Authorize] async (ClaimsPrincipal user, IUserStore userStore) =>
{
    var email = user.FindFirstValue(ClaimTypes.Email);
    if (email is null)
    {
        return Results.Unauthorized();
    }

    var userRecord = await userStore.FindByEmailAsync(email);
    return userRecord is null
        ? Results.NotFound(new { error = "User profile not found." })
        : Results.Ok(new { userRecord.Id, userRecord.Email, userRecord.Role });
});

app.Run();

static byte[] GetJwtSecretKey(IConfiguration config)
{
    var key = config["Jwt:Key"] ?? "ReplaceThisWithASecretKeyOfAtLeast32Chars!";
    return Encoding.UTF8.GetBytes(key);
}

static string BuildJwtToken(UserRecord user, IConfiguration config)
{
    var key = GetJwtSecretKey(config);
    var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"] ?? "dotnet_angular",
        audience: config["Jwt:Audience"] ?? "dotnet_angular_audience",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static string HashPassword(string password)
{
    var salt = RandomNumberGenerator.GetBytes(16);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
}

static bool VerifyPassword(string password, string storedHash)
{
    var parts = storedHash.Split('.', 2);
    if (parts.Length != 2)
    {
        return false;
    }

    var salt = Convert.FromBase64String(parts[0]);
    var expectedHash = Convert.FromBase64String(parts[1]);
    var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
}

