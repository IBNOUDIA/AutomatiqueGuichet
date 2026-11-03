namespace GuichetAutomatique.Models;

/// <summary>
/// Regroupe les données bancaires chargées en mémoire pendant une opération.
/// La persistance est assurée par Entity Framework Core dans la base de données.
/// </summary>
public class BanqueData
{
    public List<Client> Clients { get; set; } = new();
    public List<CompteBancaire> Comptes { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
    public Guichet Guichet { get; set; } = new();
}
