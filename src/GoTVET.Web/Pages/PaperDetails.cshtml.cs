using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GoTVET.Web.Pages;

public class PaperDetailsModel : PageModel
{
    private readonly PaperLibrary _library;

    public PaperDetailsModel(PaperLibrary library)
    {
        _library = library;
    }

    public PastPaper? Paper { get; private set; }

    public IActionResult OnGet(string id)
    {
        Paper = _library.Find(id);
        return Paper is null ? NotFound() : Page();
    }
}
