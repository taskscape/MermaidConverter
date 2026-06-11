using MermaidToDiagrams.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EligibilityChecker>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new CliRunner(configuration["MermaidToDiagrams:CliPath"]);
});
builder.Services.AddSingleton<MermaidConversionService>();

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/health"));

app.MapGet("/health", (CliRunner cliRunner) =>
{
    try
    {
        return Results.Ok(new
        {
            status = "ok",
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

app.MapPost("/api/validate", async (ApiConversionRequest request, MermaidConversionService service, CancellationToken cancellationToken) =>
{
    var result = await service.ValidateAsync(request.Mermaid, cancellationToken);
    return Results.Json(new
    {
        valid = result.Success,
        issues = result.Issues.Select(ToIssueDto),
        cli = ToCliDto(result.CliResult)
    }, statusCode: result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
});

app.MapPost("/api/convert", async (ApiConversionRequest request, MermaidConversionService service, CancellationToken cancellationToken) =>
{
    var result = await service.ConvertAsync(new ConversionRequest(request.Mermaid, request.Format, request.Theme), cancellationToken);
    if (!result.Success || result.DiagramBytes is null || result.ContentType is null)
    {
        return Results.Json(ToErrorDto(result), statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var fileName = "diagram." + result.Format;
    return Results.File(result.DiagramBytes, result.ContentType, fileName);
});

app.MapPost("/api/convert/base64", async (ApiConversionRequest request, MermaidConversionService service, CancellationToken cancellationToken) =>
{
    var result = await service.ConvertAsync(new ConversionRequest(request.Mermaid, request.Format, request.Theme), cancellationToken);
    if (!result.Success || result.DiagramBytes is null)
    {
        return Results.Json(ToErrorDto(result), statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    return Results.Ok(new
    {
        format = result.Format,
        contentType = result.ContentType,
        diagramBase64 = Convert.ToBase64String(result.DiagramBytes),
        pythonScript = request.IncludePython ? result.PythonScript : null,
        cli = ToCliDto(result.CliResult)
    });
});

app.Run();

static object ToIssueDto(EligibilityIssue issue)
{
    return new
    {
        severity = issue.Severity.ToString().ToLowerInvariant(),
        message = issue.Message
    };
}

static object? ToCliDto(CliRunResult? result)
{
    return result is null
        ? null
        : new
        {
            exitCode = result.ExitCode,
            commandLine = result.CommandLine,
            standardOutput = result.StandardOutput,
            standardError = result.StandardError
        };
}

static object ToErrorDto(ConversionResult result)
{
    return new
    {
        success = false,
        errors = result.Issues.Select(ToIssueDto),
        cli = ToCliDto(result.CliResult)
    };
}

public sealed record ApiConversionRequest(
    string? Mermaid,
    string? Format = "png",
    string? Theme = "azure-modern",
    bool IncludePython = false);
