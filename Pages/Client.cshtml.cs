using GuichetAutomatique.Models;
using GuichetAutomatique.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GuichetAutomatique.Pages;

public class ClientModel : PageModel
{
    private readonly BanqueService _banque;

    public ClientModel(BanqueService banque) => _banque = banque;

    public Models.Client? ClientConnecte { get; set; }
    public List<CompteBancaire> Comptes { get; set; } = new();
    public List<Transaction> DernieresTransactions { get; set; } = new();

    public string? MessageSucces { get; set; }
    public string? MessageErreur { get; set; }

    [BindProperty] public string CompteSelectionne { get; set; } = string.Empty;
    [BindProperty] public string CompteDestination { get; set; } = string.Empty;
    [BindProperty] public decimal Montant { get; set; }

    private IActionResult? VerifierSession()
    {
        var code = HttpContext.Session.GetString(IndexModel.CleSessionClient);
        if (string.IsNullOrEmpty(code)) return RedirectToPage("/Index");

        ClientConnecte = _banque.ObtenirClient(code);
        if (ClientConnecte is null || ClientConnecte.EstBloque)
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }

        Comptes = _banque.ObtenirComptes(code);
        DernieresTransactions = Comptes
            .SelectMany(c => _banque.ObtenirTransactions(c.NumeroCompte))
            .DistinctBy(t => t.Id)
            .OrderByDescending(t => t.DateHeure)
            .Take(15)
            .ToList();

        return null;
    }

    public IActionResult OnGet()
    {
        return VerifierSession() ?? Page();
    }

    public IActionResult OnPostDeposer()
    {
        var redir = VerifierSession();
        if (redir != null) return redir;

        // Validation serveur stricte
        if (Montant <= 0)
        {
            MessageErreur = "Le montant doit être positif.";
            return Page();
        }

        if (string.IsNullOrEmpty(CompteSelectionne))
        {
            MessageErreur = "Veuillez sélectionner un compte.";
            return Page();
        }

        var resultat = _banque.Deposer(CompteSelectionne, Montant);
        AppliquerResultat(resultat);
        return Page();
    }

    public IActionResult OnPostRetirer()
    {
        var redir = VerifierSession();
        if (redir != null) return redir;

        // Validation serveur stricte
        if (Montant <= 0)
        {
            MessageErreur = "Le montant doit être positif.";
            return Page();
        }

        if (Montant % 10 != 0)
        {
            MessageErreur = "Le montant doit être un multiple de 10$.";
            return Page();
        }

        if (Montant > 1000)
        {
            MessageErreur = "Le montant maximal par retrait est 1000$.";
            return Page();
        }

        if (string.IsNullOrEmpty(CompteSelectionne))
        {
            MessageErreur = "Veuillez sélectionner un compte.";
            return Page();
        }

        var resultat = _banque.Retirer(CompteSelectionne, Montant);
        AppliquerResultat(resultat);
        return Page();
    }

    public IActionResult OnPostTransferer()
    {
        var redir = VerifierSession();
        if (redir != null) return redir;

        // Validation serveur stricte
        if (Montant <= 0)
        {
            MessageErreur = "Le montant doit être positif.";
            return Page();
        }

        if (string.IsNullOrEmpty(CompteDestination))
        {
            MessageErreur = "Veuillez sélectionner un compte de destination.";
            return Page();
        }

        var compteCheque = Comptes.FirstOrDefault(c => c.Type == TypeCompte.Cheque);
        if (compteCheque is null)
        {
            MessageErreur = "Aucun compte Chèque trouvé.";
            return Page();
        }

        var resultat = _banque.Transferer(compteCheque.NumeroCompte, CompteDestination, Montant);
        AppliquerResultat(resultat);
        return Page();
    }

    public IActionResult OnPostPayerFacture()
    {
        var redir = VerifierSession();
        if (redir != null) return redir;

        // Validation serveur stricte
        if (Montant <= 0)
        {
            MessageErreur = "Le montant doit être positif.";
            return Page();
        }

        var compteCheque = Comptes.FirstOrDefault(c => c.Type == TypeCompte.Cheque);
        if (compteCheque is null)
        {
            MessageErreur = "Aucun compte Chèque trouvé.";
            return Page();
        }

        var resultat = _banque.PayerFacture(compteCheque.NumeroCompte, Montant);
        AppliquerResultat(resultat);
        return Page();
    }

    public IActionResult OnPostDeconnexion()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Index");
    }

    private void AppliquerResultat(Resultat resultat)
    {
        if (resultat.Succes)
        {
            MessageSucces = resultat.Message;
            // Recharger les soldes/transactions à jour après la mutation.
            Comptes = _banque.ObtenirComptes(ClientConnecte!.CodeClient);
            DernieresTransactions = Comptes
                .SelectMany(c => _banque.ObtenirTransactions(c.NumeroCompte))
                .DistinctBy(t => t.Id)
                .OrderByDescending(t => t.DateHeure)
                .Take(15)
                .ToList();
        }
        else
        {
            MessageErreur = resultat.Message;
        }
    }
}
