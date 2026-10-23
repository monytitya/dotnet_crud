using backend.Data;
using backend.Models.Data;
using backend.Services;
using backend.Services.Navigation;
using backend.Services.Permission;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oracle.EntityFrameworkCore.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var cs = builder.Configuration.GetConnectionString("OracleDb");
if (string.IsNullOrWhiteSpace(cs) || cs.Contains("YOUR_USER", StringComparison.OrdinalIgnoreCase) || cs.Contains("YOUR_PASSWORD", StringComparison.OrdinalIgnoreCase) || cs.Contains("YOUR_HOST", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Configure ConnectionStrings:OracleDb using user secrets or the ConnectionStrings__OracleDb environment variable.");
builder.Services.AddDbContext<AppDbContext>(o => o.UseOracle(cs, oracle =>
    oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21)));
var key = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(key) || key.Length < 32) throw new InvalidOperationException("Set Jwt:Key to a random secret of at least 32 characters.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"], ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromSeconds(30) }; o.Events = new JwtBearerEvents { OnTokenValidated = c => { if (c.Principal?.FindFirst("token_type")?.Value != "access") c.Fail("An access token is required."); return Task.CompletedTask; } }; });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<TwoFactorService>();
builder.Services.AddCors(o => o.AddPolicy("Angular", p => p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"]).AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();
if (builder.Configuration.GetValue<bool>("Database:EnsureCreatedOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}
if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();} app.UseHttpsRedirection();app.UseCors("Angular");app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.Run();
