using System.Data;
using Dapper;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                        ?? "Data Source=profiles.db";

builder.Services.AddSingleton<IDbConnection>(_ => new SqliteConnection(connectionString));

// Initialize Firebase Admin using environment variable or JSON config file
var firebaseCredentialsPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_PATH");
if (!string.IsNullOrEmpty(firebaseCredentialsPath) && File.Exists(firebaseCredentialsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
    });
}

var app = builder.Build();

// Ensure database and table exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    db.Execute(@"
        CREATE TABLE IF NOT EXISTS user_profiles (
            Id TEXT PRIMARY KEY,
            FullName TEXT NOT NULL,
            Email TEXT NOT NULL,
            CountryCode TEXT,
            PhoneNumber TEXT,
            Country TEXT,
            Gender TEXT,
            Address TEXT,
            AvatarUrl TEXT,
            AvatarPublicId TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
    ");
}

// Simple Firebase ID Token validation helper
async Task<string?> ValidateFirebaseToken(HttpContext context)
{
    var authHeader = context.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        return null;

    var token = authHeader["Bearer ".Length..].Trim();

    if (FirebaseApp.DefaultInstance == null)
        return null;

    try
    {
        var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
        return decoded.Uid;
    }
    catch
    {
        return null;
    }
}

app.MapGet("/", () => "M4Food API running");

// 获取当前用户 profile
app.MapGet("/profiles/me", async (HttpContext ctx, IDbConnection db) =>
{
    var userId = await ValidateFirebaseToken(ctx);
    if (userId is null) return Results.Unauthorized();

    var profile = await db.QuerySingleOrDefaultAsync(@"
        SELECT * FROM user_profiles WHERE Id = @Id
    ", new { Id = userId });

    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

// 创建/更新当前用户 profile
app.MapPost("/profiles/me", async (HttpContext ctx, IDbConnection db) =>
{
    var userId = await ValidateFirebaseToken(ctx);
    if (userId is null) return Results.Unauthorized();

    var dto = await ctx.Request.ReadFromJsonAsync<UserProfileDto>();
    if (dto is null) return Results.BadRequest("Invalid body");

    dto.Id = userId;
    dto.UpdatedAt = DateTime.UtcNow;
    if (dto.CreatedAt == default) dto.CreatedAt = DateTime.UtcNow;

    const string sql = @"
        INSERT INTO user_profiles
        (Id, FullName, Email, CountryCode, PhoneNumber, Country, Gender, Address, AvatarUrl, AvatarPublicId, CreatedAt, UpdatedAt)
        VALUES (@Id, @FullName, @Email, @CountryCode, @PhoneNumber, @Country, @Gender, @Address, @AvatarUrl, @AvatarPublicId, @CreatedAt, @UpdatedAt)
        ON CONFLICT(Id) DO UPDATE SET
            FullName = excluded.FullName,
            Email = excluded.Email,
            CountryCode = excluded.CountryCode,
            PhoneNumber = excluded.PhoneNumber,
            Country = excluded.Country,
            Gender = excluded.Gender,
            Address = excluded.Address,
            AvatarUrl = excluded.AvatarUrl,
            AvatarPublicId = excluded.AvatarPublicId,
            UpdatedAt = excluded.UpdatedAt;
    ";

    await db.ExecuteAsync(sql, dto);
    return Results.Ok(dto);
});

// 删除当前用户 profile
app.MapDelete("/profiles/me", async (HttpContext ctx, IDbConnection db) =>
{
    var userId = await ValidateFirebaseToken(ctx);
    if (userId is null) return Results.Unauthorized();

    await db.ExecuteAsync("DELETE FROM user_profiles WHERE Id = @Id", new { Id = userId });
    return Results.NoContent();
});

app.Run();

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Country { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarPublicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

