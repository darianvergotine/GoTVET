using System.Text.Json;

namespace GoTVET.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<PaperLibrary>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        var app = builder.Build();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.MapRazorPages();

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

    private static string Absolute(HttpRequest request, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return $"{request.Scheme}://{request.Host}{path}";
    }
}
