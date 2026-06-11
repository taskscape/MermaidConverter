using MermaidToDiagrams.MCP;
using MermaidToDiagrams.Shared;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddSingleton<EligibilityChecker>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new CliRunner(configuration["MermaidToDiagrams:CliPath"]);
});
builder.Services.AddSingleton<MermaidConversionService>();
builder.Services.AddSingleton<MermaidDiagramMcpTools>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .AddAuthorizationFilters()
    .WithTools<MermaidDiagramMcpTools>();

var app = builder.Build();

app.UseMiddleware<OriginValidationMiddleware>();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/health"));
app.MapGet("/health", (CliRunner cliRunner) =>
{
    try
    {
        return Results.Ok(new
        {
            status = "ok",
            mcpEndpoint = "/mcp",
            transport = "Streamable HTTP",
            stateless = true,
            cliPath = cliRunner.ResolveCliPath()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "CLI unavailable",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapMcp("/mcp");

app.Run();
