namespace GuichetAutomatique.Models;

/// <summary>
/// État global du guichet automatique lui-même (indépendant des clients).
/// </summary>
public class Guichet
{
    public int Id { get; set; } = 1; // Clé primaire (il n'y a qu'un seul guichet)

    public const decimal MaximumArgentPapier = 20_000m;

    /// <summary>Argent papier physiquement présent dans le guichet.
    /// Seuls les retraits l'affectent — les dépôts n'ont aucune influence.</summary>
    public decimal ArgentPapier { get; set; } = 5_000m;

    /// <summary>Une fois fermé (par l'administrateur), plus aucune opération n'est permise.</summary>
    public bool EstFerme { get; set; }
}
