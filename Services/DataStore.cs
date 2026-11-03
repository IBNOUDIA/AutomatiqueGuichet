using GuichetAutomatique.Models;
using Microsoft.EntityFrameworkCore;

namespace GuichetAutomatique.Services;

/// <summary>
/// Couche d'accès aux données utilisant Entity Framework Core.
/// Les données sont lues et écrites dans la base configurée (SQLite en local).
/// </summary>
public class DataStore
{
    private readonly BanqueDbContext _dbContext;
    private readonly ILogger<DataStore> _logger;

    public DataStore(BanqueDbContext dbContext, ILogger<DataStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Exécute une action en lecture seule (requête SELECT).
    /// </summary>
    public T Lire<T>(Func<BanqueData, T> operation)
    {
        try
        {
            return operation(ChargerDonnees());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la lecture des données: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Exécute une action de mutation (INSERT/UPDATE/DELETE) avec transaction.
    /// </summary>
    public T Ecrire<T>(Func<BanqueData, T> operation)
    {
        using (var transaction = _dbContext.Database.BeginTransaction())
        {
            try
            {
                var data = ChargerDonnees();

                // Instantané des listes AVANT l'opération : permet de détecter les
                // ajouts/suppressions par la suite sans re-requêter la BD au complet.
                var clientsOriginaux = new List<Client>(data.Clients);
                var comptesOriginaux = new List<CompteBancaire>(data.Comptes);
                var transactionsOriginales = new List<Transaction>(data.Transactions);
                var guichetEstNouveau = _dbContext.Entry(data.Guichet).State == EntityState.Detached;

                // Exécuter l'opération
                var resultat = operation(data);

                // Persister les modifications
                PersisterModifications(data, guichetEstNouveau, clientsOriginaux, comptesOriginaux, transactionsOriginales);

                // Sauvegarder et valider la transaction
                _dbContext.SaveChanges();
                transaction.Commit();

                _logger.LogInformation($"Opération exécutée");
                return resultat;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Erreur lors de l'écriture: {ex.Message}");
                throw;
            }
        }
    }

    private BanqueData ChargerDonnees()
    {
        var guichet = _dbContext.Guichets.FirstOrDefault() ?? new Guichet { Id = 1 };
        return new BanqueData
        {
            Guichet = guichet,
            Clients = _dbContext.Clients.ToList(),
            Comptes = _dbContext.Comptes.ToList(),
            Transactions = _dbContext.Transactions.ToList()
        };
    }

    /// <summary>
    /// Les entités déjà suivies par le contexte (chargées par ChargerDonnees) voient leurs
    /// mutations détectées automatiquement par SaveChanges — seuls les ajouts/suppressions
    /// doivent être signalés explicitement, en comparant à l'instantané pris avant l'opération.
    /// </summary>
    private void PersisterModifications(BanqueData data, bool guichetEstNouveau,
        List<Client> clientsOriginaux, List<CompteBancaire> comptesOriginaux, List<Transaction> transactionsOriginales)
    {
        // Guichet
        if (guichetEstNouveau)
            _dbContext.Guichets.Add(data.Guichet);

        // Clients
        var codesClientsOriginaux = clientsOriginaux.Select(c => c.CodeClient).ToHashSet();
        foreach (var client in data.Clients)
        {
            if (!codesClientsOriginaux.Contains(client.CodeClient))
                _dbContext.Clients.Add(client);
        }
        foreach (var clientOriginal in clientsOriginaux)
        {
            if (!data.Clients.Any(c => c.CodeClient == clientOriginal.CodeClient))
                _dbContext.Clients.Remove(clientOriginal);
        }

        // Comptes
        var numerosComptesOriginaux = comptesOriginaux.Select(c => c.NumeroCompte).ToHashSet();
        foreach (var compte in data.Comptes)
        {
            if (!numerosComptesOriginaux.Contains(compte.NumeroCompte))
                _dbContext.Comptes.Add(compte);
        }
        foreach (var compteOriginal in comptesOriginaux)
        {
            if (!data.Comptes.Any(c => c.NumeroCompte == compteOriginal.NumeroCompte))
                _dbContext.Comptes.Remove(compteOriginal);
        }

        // Transactions (ajout uniquement — historique immuable)
        var idsTransactionsOriginales = transactionsOriginales.Select(t => t.Id).ToHashSet();
        foreach (var trans in data.Transactions)
        {
            if (!idsTransactionsOriginales.Contains(trans.Id))
                _dbContext.Transactions.Add(trans);
        }
    }
}
