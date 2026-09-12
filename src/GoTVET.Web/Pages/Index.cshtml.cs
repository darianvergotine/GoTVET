using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GoTVET.Web.Pages;

public class IndexModel : PageModel
{
    private readonly PaperLibrary _library;

    public IndexModel(PaperLibrary library)
    {
        _library = library;
    }

    public int PaperCount { get; private set; }
    public IReadOnlyList<string> Subjects { get; private set; } = [];
    public IReadOnlyList<string> Levels { get; private set; } = [];

    public void OnGet()
    {
        PaperCount = _library.Papers.Count;
        Subjects = _library.Subjects;
        Levels = _library.Papers.Select(paper => paper.Level).Distinct().OrderBy(value => value).ToList();
    }
}
