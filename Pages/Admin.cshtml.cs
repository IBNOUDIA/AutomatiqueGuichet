using GuichetAutomatique.Models;
using GuichetAutomatique.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GuichetAutomatique.Pages;

public class AdminModel : PageModel
{
    // Mot de passe administrateur depuis configuration (appsettings.json)
    private readonly string _motDePasseAdmin;
    private const string CleSessionAdmin = "AdminConnecte";

    private readonly BanqueService _banque;
    private readonly IConfiguration _configuration;
    private readonly AuditLogger _audit;

    public AdminModel(BanqueService banque, IConfiguration configuration, AuditLogger audit)
    {
        _banque = banque;
        _configuration = configuration;
        _audit = audit;
        // Lire depuis appsettings.json → section "Banque" → clé "AdminPassword"
        _motDePasseAdmin = _configuration["Banque:AdminPassword"] ?? "admin123";
    }

    public bool EstConnecte { get; set; }
    public string? MessageErreur { get; set; }
    public string? MessageSucces { get; set; }

    public List<Models.Client> Clients { get; set; } = new();
    public Guichet Guichet { get; set; } = new();
    public List<Transaction> TransactionsAffichees { get; set; } = new();

    [BindProperty] public string MotDePasseSaisi { get; set; } = string.Empty;

    // Création de client
    [BindProperty] public string NouveauCodeClient { get; set; } = string.Empty;
    [BindProperty] public string NouveauNom { get; set; } = string.Empty;
    [BindProperty] public string NouveauPrenom { get; set; } = string.Empty;
    [BindProperty] public string NouveauTelephone { get; set; } = string.Empty;
    [BindProperty] public string NouveauCourriel { get; set; } = string.Empty;
    [BindProperty] public string NouveauNip { get; set; } = string.Empty;
    [BindProperty] public decimal NouveauSoldeCheque { get; set; }

    // Modification de client
    [BindProperty] public string CodeClientAModifier { get; set; } = string.Empty;
    [BindProperty] public string ModifNom { get; set; } = string.Empty;
    [BindProperty] public string ModifPrenom { get; set; } = string.Empty;
    [BindProperty] public string ModifTelephone { get; set; } = string.Empty;
    [BindProperty] public string ModifCourriel { get; set; } = string.Empty;

    // Création de compte
    [BindProperty] public string CodeClientPourCompte { get; set; } = string.Empty;
    [BindProperty] public TypeCompte NouveauTypeCompte { get; set; }
    [BindProperty] public decimal SoldeInitialCompte { get; set; }

    // Blocage / déblocage
    [BindProperty] public string CodeClientCible { get; set; } = string.Empty;

    // Argent papier
    [BindProperty] public decimal MontantArgentPapier { get; set; }

    // Prélèvement hypothécaire
    [BindProperty] public string CompteHypothecaireCible { get; set; } = string.Empty;
    [BindProperty] public decimal MontantPrelevement { get; set; }

    // Consultation de l'historique par compte
    [BindProperty(SupportsGet = true)] public string? CompteConsulte { get; set; }

    private bool ChargerEtatSiConnecte()
    {
        EstConnecte = HttpContext.Session.GetString(CleSessionAdmin) == "1";
        if (!EstConnecte) return false;

        Clients = _banque.ObtenirTousLesClients();
        Guichet = _banque.ObtenirGuichet();
        TransactionsAffichees = string.IsNullOrEmpty(CompteConsulte)
            ? _banque.ObtenirToutesLesTransactions().Take(30).ToList()
            : _banque.ObtenirTransactions(CompteConsulte);

        return true;
    }

    public void OnGet()
    {
        ChargerEtatSiConnecte();
    }

    public IActionResult OnPostConnexionAdmin()
    {
        if (MotDePasseSaisi == _motDePasseAdmin)
        {
            HttpContext.Session.SetString(CleSessionAdmin, "1");
            _audit.AccesAdminReussi();
        }
        else
        {
            MessageErreur = "Mot de passe administrateur incorrect.";
            _audit.TentativeAccesAdminEchouee();
        }
        ChargerEtatSiConnecte();
        return Page();
    }

    public IActionResult OnPostDeconnexionAdmin()
    {
        HttpContext.Session.Remove(CleSessionAdmin);
        _audit.EnregistrerEvenement("ADMIN_LOGOUT", null, "Admin déconnecté");
        return RedirectToPage("/Admin");
    }

    public IActionResult OnPostCreerClient()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");

        // Validations strictes
        if (string.IsNullOrWhiteSpace(NouveauCodeClient))
        {
            MessageErreur = "Le code client est requis.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NouveauNom) || string.IsNullOrWhiteSpace(NouveauPrenom))
        {
            MessageErreur = "Le nom et prénom sont requis.";
            return Page();
        }

        if (NouveauNip.Length != 4 || !NouveauNip.All(char.IsDigit))
        {
            MessageErreur = "Le NIP doit être composé de 4 chiffres.";
            return Page();
        }

        if (NouveauSoldeCheque < 0)
        {
            MessageErreur = "Le solde initial ne peut pas être négatif.";
            return Page();
        }

        var resultat = _banque.CreerClient(NouveauCodeClient.Trim(), NouveauNom.Trim(), 
            NouveauPrenom.Trim(), NouveauTelephone.Trim(), NouveauCourriel.Trim(), 
            NouveauNip.Trim(), NouveauSoldeCheque);
        
        if (resultat.Succes)
            _audit.OperationAdmin("CREATE_CLIENT", $"Client: {NouveauCodeClient}");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostModifierClient()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");

        // Validations strictes
        if (string.IsNullOrWhiteSpace(CodeClientAModifier))
        {
            MessageErreur = "Le code client est requis.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ModifNom) || string.IsNullOrWhiteSpace(ModifPrenom))
        {
            MessageErreur = "Le nom et prénom sont requis.";
            return Page();
        }

        var resultat = _banque.ModifierClient(CodeClientAModifier.Trim(), ModifNom.Trim(), 
            ModifPrenom.Trim(), ModifTelephone.Trim(), ModifCourriel.Trim());
        
        if (resultat.Succes)
            _audit.OperationAdmin("MODIFY_CLIENT", $"Client: {CodeClientAModifier}");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostCreerCompte()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");

        // Validations strictes
        if (string.IsNullOrWhiteSpace(CodeClientPourCompte))
        {
            MessageErreur = "Le code client est requis.";
            return Page();
        }

        if (SoldeInitialCompte < 0)
        {
            MessageErreur = "Le solde initial ne peut pas être négatif.";
            return Page();
        }

        var resultat = _banque.CreerCompte(CodeClientPourCompte.Trim(), NouveauTypeCompte, SoldeInitialCompte);
        
        if (resultat.Succes)
            _audit.OperationAdmin("CREATE_ACCOUNT", $"Client: {CodeClientPourCompte}, Type: {NouveauTypeCompte}");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostBloquer()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        if (string.IsNullOrWhiteSpace(CodeClientCible))
        {
            MessageErreur = "Le code client est requis.";
            return Page();
        }

        var resultat = _banque.BasculerBlocageClient(CodeClientCible.Trim(), true);
        
        if (resultat.Succes)
            _audit.OperationAdmin("BLOCK_ACCOUNT", $"Client: {CodeClientCible}");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostDebloquer()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        if (string.IsNullOrWhiteSpace(CodeClientCible))
        {
            MessageErreur = "Le code client est requis.";
            return Page();
        }

        var resultat = _banque.BasculerBlocageClient(CodeClientCible.Trim(), false);
        
        if (resultat.Succes)
            _audit.OperationAdmin("UNLOCK_ACCOUNT", $"Client: {CodeClientCible}");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostAjouterArgentPapier()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        if (MontantArgentPapier <= 0)
        {
            MessageErreur = "Le montant doit être positif.";
            return Page();
        }

        var resultat = _banque.AjouterArgentPapier(MontantArgentPapier);
        
        if (resultat.Succes)
            _audit.OperationAdmin("ADD_CASH", $"Montant: {MontantArgentPapier:C}");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostFermerGuichet()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        var resultat = _banque.FermerGuichet();
        
        if (resultat.Succes)
            _audit.OperationAdmin("CLOSE_ATM", "Guichet fermé");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostInteretEpargne()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        var resultat = _banque.PayerInteretEpargne();
        
        if (resultat.Succes)
            _audit.OperationAdmin("PAY_INTEREST_SAVINGS", "Intérêt versé");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostInteretMargeCredit()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        var resultat = _banque.ChargerInteretMargeCredit();
        
        if (resultat.Succes)
            _audit.OperationAdmin("CHARGE_INTEREST_CREDIT_MARGIN", "Intérêt chargé");
        
        Appliquer(resultat);
        return Page();
    }

    public IActionResult OnPostPreleverHypothecaire()
    {
        if (!ChargerEtatSiConnecte()) return RedirectToPage("/Admin");
        
        if (string.IsNullOrWhiteSpace(CompteHypothecaireCible))
        {
            MessageErreur = "Le numéro de compte hypothécaire est requis.";
            return Page();
        }

        if (MontantPrelevement <= 0)
        {
            MessageErreur = "Le montant doit être positif.";
            return Page();
        }

        var resultat = _banque.PreleverHypothecaire(CompteHypothecaireCible.Trim(), MontantPrelevement);
        
        if (resultat.Succes)
            _audit.OperationAdmin("WITHDRAW_MORTGAGE", $"Compte: {CompteHypothecaireCible}, Montant: {MontantPrelevement:C}");
        
        Appliquer(resultat);
        return Page();
    }

    private void Appliquer(Resultat resultat)
    {
        if (resultat.Succes) MessageSucces = resultat.Message;
        else MessageErreur = resultat.Message;

        // Recharge les données affichées après la mutation.
        Clients = _banque.ObtenirTousLesClients();
        Guichet = _banque.ObtenirGuichet();
        TransactionsAffichees = string.IsNullOrEmpty(CompteConsulte)
            ? _banque.ObtenirToutesLesTransactions().Take(30).ToList()
            : _banque.ObtenirTransactions(CompteConsulte);
    }
}
