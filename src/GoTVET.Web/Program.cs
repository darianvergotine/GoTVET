using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

namespace GoTVET.Web;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--export-offerings", StringComparer.OrdinalIgnoreCase))
        {
            ExportOfferings(args);
            return;
        }

        var builder = WebApplication.CreateBuilder(args);
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        {
            var port = Environment.GetEnvironmentVariable("PORT");
            builder.WebHost.UseUrls(string.IsNullOrWhiteSpace(port)
                ? "http://127.0.0.1:5088"
                : $"http://0.0.0.0:{port}");
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<PaperLibrary>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.Services.GetRequiredService<PaperLibrary>();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.MapRazorPages();
        app.MapGet("/health", () => Results.Text("ok"));

        app.MapGet("/api/catalog", (PaperLibrary library, HttpRequest request) =>
        {
            var snapshot = library.Snapshot();
            foreach (var paper in snapshot.Papers)
            {
                paper.DownloadUrl = Absolute(request, paper.DownloadUrl);
                paper.BrowseUrl = Absolute(request, paper.BrowseUrl);
            }

            return Results.Json(snapshot, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        });

        app.MapGet("/api/papers/{id}/download", (string id, PaperLibrary library) =>
        {
            var paper = library.Find(id);
            if (paper is null)
            {
                return Results.NotFound();
            }

            var path = library.FilePath(paper);
            if (!File.Exists(path))
            {
                return Results.NotFound();
            }

            return Results.File(path, "application/pdf", paper.FileName);
        });

        app.Run();
    }

    private static void ExportOfferings(string[] args)
    {
        var outputIndex = Array.FindIndex(args, value => value.Equals("--export-offerings", StringComparison.OrdinalIgnoreCase));
        var output = outputIndex >= 0 && outputIndex + 1 < args.Length && !args[outputIndex + 1].StartsWith('-')
            ? args[outputIndex + 1]
            : Path.Combine("docs", "data", "offerings.json");

        var payload = new
        {
            firstYear = NcvCurriculum.FirstYear,
            lastYear = NcvCurriculum.LastYear,
            levels = NcvCurriculum.Levels,
            sessions = NcvCurriculum.Sessions,
            documentTypes = NcvCurriculum.DocumentTypes,
            offerings = NcvCurriculum.Offerings
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
        Console.WriteLine($"Wrote {payload.offerings.Count} offerings to {Path.GetFullPath(output)}");
    }

    private static string Absolute(HttpRequest request, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return $"{request.Scheme}://{request.Host}{path}";
    }
}
