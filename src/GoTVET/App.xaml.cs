using System.Net;
using System.Text.Json;
using System.Windows;

namespace GoTVET;

public partial class App : System.Windows.Application
{
    public static MainViewModel ViewModel { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true
        };
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(ReadApiBaseUrl()),
            Timeout = TimeSpan.FromMinutes(8)
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GoTVET/1.0 (Windows; GoTVET library)");

        ViewModel = new MainViewModel(new CatalogService(http), new DownloadService(http));
        ThemeService.Load();
        base.OnStartup(e);
    }

    private static string ReadApiBaseUrl()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(path))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("ApiBaseUrl", out var url))
            {
                var value = url.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.TrimEnd('/') + "/";
                }
            }
        }

        return "http://127.0.0.1:5088/";
    }
}
