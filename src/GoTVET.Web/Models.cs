namespace GoTVET.Web;

public sealed class PastPaper
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Programme { get; set; } = "";
    public string Level { get; set; } = "";
    public string Field { get; set; } = "";
    public int Year { get; set; }
    public string Session { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string Language { get; set; } = "English";
    public string FileName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string BrowseUrl { get; set; } = "";
    public string Source { get; set; } = "GoTVET";
    public long? FileSizeBytes { get; set; }
}

public sealed class CatalogSnapshot
{
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Source { get; set; } = "GoTVET";
    public List<PastPaper> Papers { get; set; } = [];
}
