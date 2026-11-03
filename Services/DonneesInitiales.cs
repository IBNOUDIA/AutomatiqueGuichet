using GuichetAutomatique.Models;

namespace GuichetAutomatique.Services;

/// <summary>
/// Données de démonstration créées la toute première fois que l'application
/// démarre (base de données vide). Permet de tester immédiatement sans passer
/// par l'écran administrateur.
/// </summary>
public static class DonneesInitiales
{
    public static BanqueData Creer()
    {
        var data = new BanqueData
        {
            Guichet = new Guichet { ArgentPapier = 8_000m, EstFerme = false }
        };

        // --- Client de démonstration 1 : profil complet (4 types de comptes) ---
        var client1 = new Client
        {
            CodeClient = "1001",
            Nom = "Diallo",
            Prenom = "Amadou",
            Telephone = "514-555-0101",
            Courriel = "amadou.diallo@example.com",
            NipHache = SecurityHelper.HasherNip("1234"),
            EstBloque = false,
            TentativesEchoueesConnexion = 0
        };

        var cheque1 = new CompteBancaire { CodeClient = "1001", Type = TypeCompte.Cheque, Solde = 1500m };
        var epargne1 = new CompteBancaire { CodeClient = "1001", Type = TypeCompte.Epargne, Solde = 3200m };
        var hypo1 = new CompteBancaire { CodeClient = "1001", Type = TypeCompte.Hypothecaire, Solde = 150000m };
        var marge1 = new CompteBancaire { CodeClient = "1001", Type = TypeCompte.MargeCredit, Solde = 0m };

        // --- Client de démonstration 2 : compte chèque seulement ---
        var client2 = new Client
        {
            CodeClient = "1002",
            Nom = "Fall",
            Prenom = "Fatou",
            Telephone = "514-555-0102",
            Courriel = "fatou.fall@example.com",
            NipHache = SecurityHelper.HasherNip("5678"),
            EstBloque = false,
            TentativesEchoueesConnexion = 0
        };
        var cheque2 = new CompteBancaire { CodeClient = "1002", Type = TypeCompte.Cheque, Solde = 400m };

        data.Clients.AddRange(new[] { client1, client2 });
        data.Comptes.AddRange(new[] { cheque1, epargne1, hypo1, marge1, cheque2 });

        return data;
    }
}
