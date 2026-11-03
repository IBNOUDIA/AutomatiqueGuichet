using GuichetAutomatique.Models;

namespace GuichetAutomatique.Services;

/// <summary>
/// Résultat générique d'une opération : succès/échec + message à afficher au client.
/// </summary>
public record Resultat(bool Succes, string Message)
{
    public static Resultat Ok(string message) => new(true, message);
    public static Resultat Echec(string message) => new(false, message);
}

public record ResultatConnexion(bool Succes, bool Bloque, int EssaisRestants, string Message);

/// <summary>
/// Contient TOUTES les règles d'affaires du guichet, telles que décrites dans
/// les spécifications (frais, plafonds, marge de crédit, etc.). Les Pages Razor
/// ne font qu'appeler ces méthodes — elles ne contiennent aucune logique métier.
/// </summary>
public partial class BanqueService
{
    private const decimal FraisPaiementFacture = 1.25m;
    private const decimal MultipleRetrait = 10m;
    private const decimal MaximumRetrait = 1000m;
    private const decimal TauxInteretEpargne = 0.01m;   // 1%
    private const decimal TauxInteretMargeCredit = 0.05m; // 5%
    private const int TentativesMaximales = 3;

    private readonly DataStore _store;
    private readonly AuditLogger _audit;

    // Compteur de tentatives de connexion en mémoire (par code client).
    // Volontairement non persisté : redémarrer l'application réinitialise les compteurs,
    // mais un blocage confirmé (EstBloque = true) est, lui, bien persisté sur le client.
    // NOTE: Cela a été amélioré - les tentatives sont maintenant persistées dans Client.TentativesEchoueesConnexion
    private static readonly Dictionary<string, int> TentativesEchouees = new();

    public BanqueService(DataStore store, AuditLogger audit)
    {
        _store = store;
        _audit = audit;
    }

    // ---------------------------------------------------------------------
    // CONNEXION
    // ---------------------------------------------------------------------

    public ResultatConnexion SeConnecter(string codeClient, string nip)
    {
        return _store.Ecrire(data =>
        {
            var client = data.Clients.FirstOrDefault(c => c.CodeClient == codeClient);

            if (client is null)
            {
                _audit.EnregistrerEvenement("LOGIN_ATTEMPT_INVALID_CODE", codeClient, "Code client inconnu");
                return new ResultatConnexion(false, false, 0, "Code de client inconnu.");
            }

            if (client.EstBloque)
            {
                _audit.EnregistrerEvenement("LOGIN_ATTEMPT_BLOCKED_ACCOUNT", codeClient, "Compte bloqué");
                return new ResultatConnexion(false, true, 0,
                    "Ce compte a été bloqué. Veuillez contacter votre banque.");
            }

            // Vérifier le NIP avec comparaison hachée (sécurisée)
            if (SecurityHelper.VerifierNip(nip, client.NipHache))
            {
                // Réinitialiser les tentatives échouées
                client.TentativesEchoueesConnexion = 0;
                TentativesEchouees.Remove(codeClient);
                _audit.ConnexionReussie(codeClient);
                return new ResultatConnexion(true, false, TentativesMaximales, "Connexion réussie.");
            }

            // NIP incorrect : incrémenter le compteur de tentatives
            client.TentativesEchoueesConnexion++;
            TentativesEchouees[codeClient] = client.TentativesEchoueesConnexion;

            if (client.TentativesEchoueesConnexion >= TentativesMaximales)
            {
                client.EstBloque = true;
                TentativesEchouees.Remove(codeClient);
                _audit.CompteBloque(codeClient);
                return new ResultatConnexion(false, true, 0,
                    "Ce compte a été bloqué. Veuillez contacter votre banque.");
            }

            var essaisRestants = TentativesMaximales - client.TentativesEchoueesConnexion;
            _audit.TentativeConnexionEchouee(codeClient, essaisRestants);
            return new ResultatConnexion(false, false, essaisRestants,
                $"NIP incorrect. Il vous reste {essaisRestants} essai(s).");
        });
    }

    // ---------------------------------------------------------------------
    // CONSULTATION
    // ---------------------------------------------------------------------

    public Client? ObtenirClient(string codeClient) =>
        _store.Lire(data => data.Clients.FirstOrDefault(c => c.CodeClient == codeClient));

    public List<CompteBancaire> ObtenirComptes(string codeClient) =>
        _store.Lire(data => data.Comptes.Where(c => c.CodeClient == codeClient).ToList());

    public CompteBancaire? ObtenirCompte(string numeroCompte) =>
        _store.Lire(data => data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompte));

    public List<Transaction> ObtenirTransactions(string numeroCompte) =>
        _store.Lire(data => data.Transactions
            .Where(t => t.CompteSource == numeroCompte || t.CompteDestination == numeroCompte)
            .OrderByDescending(t => t.DateHeure)
            .ToList());

    public List<Transaction> ObtenirToutesLesTransactions() =>
        _store.Lire(data => data.Transactions.OrderByDescending(t => t.DateHeure).ToList());

    public Guichet ObtenirGuichet() => _store.Lire(data => data.Guichet);

    public List<Client> ObtenirTousLesClients() =>
        _store.Lire(data => data.Clients.OrderBy(c => c.CodeClient).ToList());
}
