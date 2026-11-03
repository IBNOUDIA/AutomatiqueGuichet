namespace GuichetAutomatique.Models;

/// <summary>
/// Les quatre types de comptes supportés par le guichet.
/// </summary>
public enum TypeCompte
{
    Cheque,
    Epargne,
    Hypothecaire,
    MargeCredit
}

/// <summary>
/// Les quatre types de transactions qu'un client peut exécuter,
/// plus les opérations réservées à l'administrateur.
/// </summary>
public enum TypeTransaction
{
    Depot,
    Retrait,
    Transfert,
    PaiementFacture,
    InteretEpargne,
    InteretMargeCredit,
    PrelevementHypothecaire,
    AjoutArgentPapier
}
