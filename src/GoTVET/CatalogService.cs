using System.Text.Json;

namespace GoTVET;

public sealed class CatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly string _cachePath;

    public string WebsiteUrl => (_http.BaseAddress?.ToString() ?? "http://127.0.0.1:5088/").TrimEnd('/');

    public CatalogService(HttpClient http)
    {
        _http = http;
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoTVET");
        Directory.CreateDirectory(folder);
        _cachePath = Path.Combine(folder, "gotvet-catalog.json");
    }

    public CatalogSnapshot LoadLocal()
    {
        if (File.Exists(_cachePath))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<CatalogSnapshot>(File.ReadAllText(_cachePath), JsonOptions);
                if (cached?.Papers.Count > 0)
                {
                    return cached;
                }
            }
            catch (JsonException)
            {
            }
        }

        return new CatalogSnapshot
        {
            UpdatedUtc = DateTimeOffset.UtcNow,
            Source = "Waiting for GoTVET website",
            Papers = []
        };
    }

    public async Task<CatalogSnapshot> RefreshAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        progress?.Report("Connecting to the GoTVET library…");
        using var response = await _http.GetAsync("api/catalog", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var snapshot = await JsonSerializer.DeserializeAsync<CatalogSnapshot>(stream, JsonOptions, cancellationToken)
                       .ConfigureAwait(false)
                       ?? new CatalogSnapshot();
        snapshot.Source = string.IsNullOrWhiteSpace(snapshot.Source) ? "GoTVET library" : snapshot.Source;
        await File.WriteAllTextAsync(_cachePath, JsonSerializer.Serialize(snapshot, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
        return snapshot;
    }
}
