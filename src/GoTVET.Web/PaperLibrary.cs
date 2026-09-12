namespace GoTVET.Web;

public sealed class PaperLibrary
{
    private readonly string _storagePath;
    private readonly List<PastPaper> _papers = [];
    private DateTimeOffset _updatedUtc = DateTimeOffset.UtcNow;

    public PaperLibrary(IWebHostEnvironment environment)
    {
        _storagePath = Path.Combine(environment.ContentRootPath, "storage", "papers");
        Directory.CreateDirectory(_storagePath);
        RebuildCatalog();
    }

    public CatalogSnapshot Snapshot() => new()
    {
        UpdatedUtc = _updatedUtc,
        Source = "GoTVET library",
        Papers = _papers.Select(Clone).ToList()
    };

    public IReadOnlyList<PastPaper> Papers => _papers;

    public IReadOnlyList<string> Subjects =>
        _papers.Select(paper => paper.Subject).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();

    public PastPaper? Find(string id) =>
        _papers.FirstOrDefault(paper => string.Equals(paper.Id, id, StringComparison.OrdinalIgnoreCase));

    public string FilePath(PastPaper paper) => Path.Combine(_storagePath, paper.FileName);

    public IEnumerable<PastPaper> Search(string? query, string? subject, string? level, string? type)
    {
        IEnumerable<PastPaper> papers = _papers;
        if (!string.IsNullOrWhiteSpace(subject) && subject != "all")
        {
            papers = papers.Where(paper => paper.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(level) && level != "all")
        {
            papers = papers.Where(paper => paper.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
        {
            papers = papers.Where(paper => paper.DocumentType.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            papers = papers.Where(paper =>
                paper.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                paper.Subject.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                paper.Level.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return papers;
    }

    private void RebuildCatalog()
    {
        _papers.Clear();
        foreach (var subject in new[] { "Mathematics", "English" })
        {
            foreach (var level in new[] { "L2", "L3", "L4" })
            {
                foreach (var documentType in new[] { "Question Paper", "Memorandum" })
                {
                    _papers.Add(CreatePlaceholder(subject, level, documentType));
                }
            }
        }

        _updatedUtc = DateTimeOffset.UtcNow;
    }

    private PastPaper CreatePlaceholder(string subject, string level, string documentType)
    {
        var slugType = documentType.Equals("Memorandum", StringComparison.OrdinalIgnoreCase) ? "memo" : "qp";
        var id = $"ncv-{Slug(subject)}-{level.ToLowerInvariant()}-{slugType}-nov-2024";
        var fileName = $"{id}.pdf";
        var path = Path.Combine(_storagePath, fileName);
        var heading = $"NC(V) {subject} {level}";
        var details = $"{documentType} · November 2024 · Placeholder";
        File.WriteAllBytes(path, PlaceholderPdf.Create(heading, details));

        return new PastPaper
        {
            Id = id,
            Title = $"{subject} {level} · {documentType} · November 2024",
            Subject = subject,
            Programme = "NC(V)",
            Level = level,
            Field = "Fundamentals",
            Year = 2024,
            Session = "November",
            DocumentType = documentType,
            Language = "English",
            FileName = fileName,
            DownloadUrl = $"/api/papers/{id}/download",
            BrowseUrl = $"/papers/{id}",
            Source = "GoTVET",
            FileSizeBytes = new FileInfo(path).Length
        };
    }

    private static PastPaper Clone(PastPaper paper) => new()
    {
        Id = paper.Id,
        Title = paper.Title,
        Subject = paper.Subject,
        Programme = paper.Programme,
        Level = paper.Level,
        Field = paper.Field,
        Year = paper.Year,
        Session = paper.Session,
        DocumentType = paper.DocumentType,
        Language = paper.Language,
        FileName = paper.FileName,
        DownloadUrl = paper.DownloadUrl,
        BrowseUrl = paper.BrowseUrl,
        Source = paper.Source,
        FileSizeBytes = paper.FileSizeBytes
    };

    private static string Slug(string value) =>
        new string(value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray())
            .Trim('-');
}
