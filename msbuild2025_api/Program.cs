using System.Data.SqlClient;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();

// Enable Swagger UI
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Minimal API endpoint to get session by code
app.MapGet("/session/{code}", async (string code, IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = @"SELECT TOP 1 * FROM [msbuild2025].[dbo].[sessions] WHERE session_code = @code";
    var session = await connection.QueryFirstOrDefaultAsync<Session>(sql, new { code });
    return session is not null ? Results.Ok(session) : Results.NotFound();
})
.WithName("GetSessionByCode");

// Get all sessions API
app.MapGet("/sessions", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT * FROM [msbuild2025].[dbo].[sessions]";
    var sessions = await connection.QueryAsync<Session>(sql);
    return Results.Ok(sessions.ToList());
});

// Flexible filter API for any slicer combination with partial (LIKE) matching
app.MapGet("/sessions/filter", async (
    IConfiguration config,
    string? topic,
    string? tag,
    string? learningCategory,
    string? sessionLevel,
    string? sessionType,
    string? speakerName
) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var whereClauses = new List<string>();
    var parameters = new DynamicParameters();
    if (!string.IsNullOrWhiteSpace(topic))
    {
        whereClauses.Add("topic LIKE '%' + @topic + '%'");
        parameters.Add("topic", topic);
    }
    if (!string.IsNullOrWhiteSpace(tag))
    {
        whereClauses.Add("tags LIKE '%' + @tag + '%'");
        parameters.Add("tag", tag);
    }
    if (!string.IsNullOrWhiteSpace(learningCategory))
    {
        whereClauses.Add("nextstep_category LIKE '%' + @learningCategory + '%'");
        parameters.Add("learningCategory", learningCategory);
    }
    if (!string.IsNullOrWhiteSpace(sessionLevel))
    {
        whereClauses.Add("session_level LIKE '%' + @sessionLevel + '%'");
        parameters.Add("sessionLevel", sessionLevel);
    }
    if (!string.IsNullOrWhiteSpace(sessionType))
    {
        whereClauses.Add("session_type LIKE '%' + @sessionType + '%'");
        parameters.Add("sessionType", sessionType);
    }
    if (!string.IsNullOrWhiteSpace(speakerName))
    {
        whereClauses.Add("speaker_name LIKE '%' + @speakerName + '%'");
        parameters.Add("speakerName", speakerName);
    }
    var sql = "SELECT * FROM [msbuild2025].[dbo].[sessions]";
    if (whereClauses.Count > 0)
    {
        sql += " WHERE " + string.Join(" AND ", whereClauses);
    }
    var sessions = await connection.QueryAsync<Session>(sql, parameters);
    return Results.Ok(sessions.ToList());
});

// API for dashboard stats
app.MapGet("/stats/sessions", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT COUNT(*) FROM [msbuild2025].[dbo].[sessions]";
    var count = await connection.ExecuteScalarAsync<int>(sql);
    return Results.Ok(count);
});

app.MapGet("/stats/speakers", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT COUNT(DISTINCT speaker_name) FROM [msbuild2025].[dbo].[sessions] WHERE speaker_name IS NOT NULL AND speaker_name <> ''";
    var count = await connection.ExecuteScalarAsync<int>(sql);
    return Results.Ok(count);
});

app.MapGet("/stats/selflearning", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT COUNT(*) FROM [msbuild2025].[dbo].[sessions] WHERE nextstep_category IS NOT NULL AND nextstep_category <> ''";
    var count = await connection.ExecuteScalarAsync<int>(sql);
    return Results.Ok(count);
});

app.MapGet("/stats/hours", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT SUM(duration_minutes) FROM [msbuild2025].[dbo].[sessions]";
    var totalMinutes = await connection.ExecuteScalarAsync<int?>(sql) ?? 0;
    var totalHours = totalMinutes / 60;
    return Results.Ok(totalHours);
});

app.MapGet("/stats/recorded", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT COUNT(*) FROM [msbuild2025].[dbo].[sessions] WHERE recorded_status IS NOT NULL AND recorded_status <> ''";
    var count = await connection.ExecuteScalarAsync<int>(sql);
    return Results.Ok(count);
});

// Slicer APIs

app.MapGet("/slicers/tags", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT DISTINCT tags FROM [msbuild2025].[dbo].[sessions] WHERE tags IS NOT NULL AND tags <> ''";
    var result = await connection.QueryAsync<string>(sql);
    return Results.Ok(result.ToList());
});

app.MapGet("/slicers/learningcategories", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT DISTINCT nextstep_category FROM [msbuild2025].[dbo].[sessions] WHERE nextstep_category IS NOT NULL AND nextstep_category <> ''";
    var result = await connection.QueryAsync<string>(sql);
    return Results.Ok(result.ToList());
});

app.MapGet("/slicers/sessionlevels", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT DISTINCT session_level FROM [msbuild2025].[dbo].[sessions] WHERE session_level IS NOT NULL AND session_level <> ''";
    var result = await connection.QueryAsync<string>(sql);
    return Results.Ok(result.ToList());
});

app.MapGet("/slicers/sessiontypes", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT DISTINCT session_type FROM [msbuild2025].[dbo].[sessions] WHERE session_type IS NOT NULL AND session_type <> ''";
    var result = await connection.QueryAsync<string>(sql);
    return Results.Ok(result.ToList());
});

app.MapGet("/slicers/speakernames", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    using var connection = new SqlConnection(connectionString);
    var sql = "SELECT DISTINCT speaker_name FROM [msbuild2025].[dbo].[sessions] WHERE speaker_name IS NOT NULL AND speaker_name <> ''";
    var result = await connection.QueryAsync<string>(sql);
    return Results.Ok(result.ToList());
});

// Redirect root to Swagger UI
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();

// Session model matching the SQL schema
public record Session(
    int id,
    string session_code,
    string title,
    string? description,
    string? speaker_name,
    int? duration_minutes,
    string? session_type,
    string? nextstep_link,
    string? nextstep_category,
    int? index_value,
    string? session_level,
    string? tags,
    string? recorded_status,
    string? session_web_link,
    string? slidedeck_link
);
