namespace GuichetAutomatique.Models;

/// <summary>
/// Représente un compte bancaire appartenant à un client.
/// Une seule classe concrète est utilisée (plutôt qu'une hiérarchie d'héritage)
/// afin de simplifier la sérialisation JSON ; le comportement spécifique à
/// chaque <see cref="TypeCompte"/> est appliqué dans BanqueService.
/// </summary>
public class CompteBancaire
{
    public string NumeroCompte { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public string CodeClient { get; set; } = string.Empty;
    public TypeCompte Type { get; set; }

    /// <summary>
    /// Pour un compte Chèque/Épargne/Hypothécaire : le solde en dollars.
    /// Pour une Marge de crédit : un solde positif indique une dette
    /// (voir spécifications — 0 = pas de dettes).
    /// </summary>
    public decimal Solde { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.Now;
}
