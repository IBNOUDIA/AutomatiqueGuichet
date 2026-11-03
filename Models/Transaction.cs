namespace GuichetAutomatique.Models;

/// <summary>
/// Historique immuable d'une opération. Chaque transaction est sauvegardée
/// individuellement (jamais seulement le solde final) pour permettre de
/// retracer l'historique complet en cas de plainte d'un client.
/// </summary>
public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime DateHeure { get; set; } = DateTime.Now;
    public TypeTransaction Type { get; set; }
    public decimal Montant { get; set; }

    /// <summary>Compte principal affecté (débité, ou crédité pour un dépôt).</summary>
    public string CompteSource { get; set; } = string.Empty;

    /// <summary>Rempli seulement pour un transfert (compte destination).</summary>
    public string? CompteDestination { get; set; }

    /// <summary>Explication lisible (ex. "Retrait avec recours à la marge de crédit").</summary>
    public string Description { get; set; } = string.Empty;

    public string CodeClient { get; set; } = string.Empty;
}
