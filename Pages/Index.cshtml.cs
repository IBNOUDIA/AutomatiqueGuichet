using GuichetAutomatique.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GuichetAutomatique.Pages;

public class IndexModel : PageModel
{
    private readonly BanqueService _banque;
    private readonly AuditLogger _audit;
    public const string CleSessionClient = "CodeClientConnecte";

    public IndexModel(BanqueService banque, AuditLogger audit)
    {
        _banque = banque;
        _audit = audit;
    }

    [BindProperty]
    public string CodeClient { get; set; } = string.Empty;

    [BindProperty]
    public string Nip { get; set; } = string.Empty;

    public string? MessageErreur { get; set; }

    public void OnGet()
    {
        // Si le guichet est fermé par l'administrateur, on avertit dès l'écran d'accueil.
        if (_banque.ObtenirGuichet().EstFerme)
        {
            MessageErreur = "Le guichet est actuellement fermé. Veuillez réessayer plus tard.";
        }
    }

    public IActionResult OnPost()
    {
        if (_banque.ObtenirGuichet().EstFerme)
        {
            MessageErreur = "Le guichet est actuellement fermé. Veuillez réessayer plus tard.";
            return Page();
        }

        var resultat = _banque.SeConnecter(CodeClient.Trim(), Nip.Trim());

        if (!resultat.Succes)
        {
            MessageErreur = resultat.Message;
            return Page();
        }

        HttpContext.Session.SetString(CleSessionClient, CodeClient.Trim());
        return RedirectToPage("/Client");
    }
}
