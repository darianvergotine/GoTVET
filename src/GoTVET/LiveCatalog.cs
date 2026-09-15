namespace GoTVET;

internal sealed class OfferingRecord
{
    public string Programme { get; set; } = "";
    public string Level { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Field { get; set; } = "";
    public string Language { get; set; } = "English";
}

internal sealed class OfferingCatalog
{
    public int FirstYear { get; set; } = 2011;
    public int LastYear { get; set; } = 2026;
    public string[] Levels { get; set; } = ["L2", "L3", "L4"];
    public string[] Sessions { get; set; } = ["February", "November"];
    public string[] DocumentTypes { get; set; } = ["Question Paper", "Memorandum"];
    public List<OfferingRecord> Offerings { get; set; } = [];
}

internal static class LiveCatalog
{
    public static CatalogSnapshot Expand(OfferingCatalog data, string websiteUrl)
    {
        var site = websiteUrl.TrimEnd('/');
        var papers = new List<PastPaper>();
        for (var year = data.FirstYear; year <= data.LastYear; year++)
        {
            foreach (var offering in data.Offerings)
            {
                foreach (var session in data.Sessions)
                {
                    foreach (var documentType in data.DocumentTypes)
                    {
                        papers.Add(CreatePaper(offering, year, session, documentType, site));
                    }
                }
            }
        }

        return new CatalogSnapshot
        {
            UpdatedUtc = DateTimeOffset.UtcNow,
            Source = "GoTVET live library",
            Papers = papers
        };
    }

    private static PastPaper CreatePaper(
        OfferingRecord offering,
        int year,
        string session,
        string documentType,
        string site)
    {
        var slugType = documentType.Equals("Memorandum", StringComparison.OrdinalIgnoreCase) ? "memo" : "qp";
        var id = $"ncv-{Slug(offering.Programme)}-{Slug(offering.Subject)}-{offering.Level.ToLowerInvariant()}-{session.ToLowerInvariant()}-{year}-{slugType}";
        return new PastPaper
        {
            Id = id,
            Title = $"{offering.Subject} {offering.Level} · {documentType} · {session} {year}",
            Subject = offering.Subject,
            Programme = offering.Programme,
            Level = offering.Level,
            Field = offering.Field,
            Year = year,
            Session = session,
            DocumentType = documentType,
            Language = string.IsNullOrWhiteSpace(offering.Language) ? "English" : offering.Language,
            FileName = $"{id}.pdf",
            DownloadUrl = "",
            BrowseUrl = $"{site}/paper.html?id={Uri.EscapeDataString(id)}",
            Source = "GoTVET"
        };
    }

    private static string Slug(string value)
    {
        var slug = new string(value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
