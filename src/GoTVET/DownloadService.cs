using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace GoTVET;

public sealed class DownloadService
{
    private readonly HttpClient _http;

    public DownloadService(HttpClient http)
    {
        _http = http;
    }

    public string DownloadFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "GoTVET",
        "Past Papers");

    public async Task<string> DownloadAsync(
        PastPaper paper,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DownloadFolder);
        if (!IsRemoteFile(paper.DownloadUrl))
        {
            return SaveLocalPlaceholder(paper, progress);
        }

        var destination = UniquePath(Path.Combine(DownloadFolder, SafeFileName(paper)));
        using var request = new HttpRequestMessage(HttpMethod.Get, paper.DownloadUrl);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                "The GoTVET server did not return this file. Open the website and try again.");
        }

        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The GoTVET server returned a web page instead of the file. Open the website and try again.");
        }

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(destination);
        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            readTotal += read;
            if (total is > 0)
            {
                progress?.Report(readTotal / (double)total.Value);
            }
        }

        progress?.Report(1);
        return destination;
    }

    public static void OpenFile(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    public static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private string SaveLocalPlaceholder(PastPaper paper, IProgress<double>? progress)
    {
        var destination = UniquePath(Path.Combine(DownloadFolder, SafeFileName(paper)));
        var heading = $"NC(V) {paper.Subject} {paper.Level}";
        var details = $"{paper.Programme} · {paper.Field} · {paper.DocumentType} · {paper.Session} {paper.Year}";
        File.WriteAllBytes(destination, PlaceholderPdf.Create(heading, details));
        progress?.Report(1);
        return destination;
    }

    private static bool IsRemoteFile(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
        url.Contains("/api/papers/", StringComparison.OrdinalIgnoreCase);

    public void OpenDownloadFolder()
    {
        Directory.CreateDirectory(DownloadFolder);
        Process.Start(new ProcessStartInfo(DownloadFolder) { UseShellExecute = true });
    }

    private static string SafeFileName(PastPaper paper)
    {
        var name = string.IsNullOrWhiteSpace(paper.FileName)
            ? $"{paper.Subject} {paper.Level} {paper.DocumentType} {paper.Session} {paper.Year}.pdf"
            : paper.FileName;
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(character, '_');
        }

        return name;
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? "";
        var stem = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 2; index < 1000; index++)
        {
            var candidate = Path.Combine(directory, $"{stem} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return path;
    }
}
