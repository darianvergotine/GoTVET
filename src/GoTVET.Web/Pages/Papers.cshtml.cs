using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GoTVET.Web.Pages;

public class PapersModel : PageModel
{
    private readonly PaperLibrary _library;

    public PapersModel(PaperLibrary library)
    {
        _library = library;
    }

    public string? Query { get; private set; }
    public string Subject { get; private set; } = "all";
    public string Level { get; private set; } = "all";
    public string Type { get; private set; } = "all";
    public IReadOnlyList<PastPaper> Papers { get; private set; } = [];
    public IReadOnlyList<string> Subjects { get; private set; } = [];

    public void OnGet(string? q, string? subject, string? level, string? type)
    {
        Query = q;
        Subject = string.IsNullOrWhiteSpace(subject) ? "all" : subject;
        Level = string.IsNullOrWhiteSpace(level) ? "all" : level;
        Type = string.IsNullOrWhiteSpace(type) ? "all" : type;
        Subjects = _library.Subjects;
        Papers = _library.Search(q, Subject, Level, Type).ToList();
    }
}
