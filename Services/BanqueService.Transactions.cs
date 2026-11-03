using GuichetAutomatique.Models;

namespace GuichetAutomatique.Services;

/// <summary>
/// Les quatre transactions que le CLIENT peut exécuter (voir spécifications) :
/// Dépôt, Retrait, Transfert, Paiement de facture.
/// </summary>
public partial class BanqueService
{
    public Resultat Deposer(string numeroCompte, decimal montant)
    {
        if (montant <= 0) return Resultat.Echec("Le montant doit être positif.");

        return _store.Ecrire(data =>
        {
            if (data.Guichet.EstFerme) return Resultat.Echec("Le guichet est fermé.");

            var compte = data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompte);
            if (compte is null) return Resultat.Echec("Compte introuvable.");

            // Un dépôt n'est permis que dans Chèque, Épargne ou Hypothécaire.
            if (compte.Type is not (TypeCompte.Cheque or TypeCompte.Epargne or TypeCompte.Hypothecaire))
                return Resultat.Echec("Un dépôt n'est pas permis sur ce type de compte.");

            compte.Solde += montant;
            // Les dépôts n'ont aucune influence sur l'argent papier du guichet.

            EnregistrerTransaction(data, TypeTransaction.Depot, montant, compte.NumeroCompte,
                null, compte.CodeClient, $"Dépôt de {montant:C} au compte {compte.Type}.");

            return Resultat.Ok($"Dépôt de {montant:C} effectué avec succès.");
        });
    }

    public Resultat Retirer(string numeroCompte, decimal montant)
    {
        if (montant <= 0) return Resultat.Echec("Le montant doit être positif.");
        if (montant % MultipleRetrait != 0) return Resultat.Echec($"Le montant doit être un multiple de {MultipleRetrait:C}.");
        if (montant > MaximumRetrait) return Resultat.Echec($"Impossible de retirer plus de {MaximumRetrait:C} par transaction.");

        return _store.Ecrire(data =>
        {
            if (data.Guichet.EstFerme) return Resultat.Echec("Le guichet est fermé.");

            var compte = data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompte);
            if (compte is null) return Resultat.Echec("Compte introuvable.");

            if (compte.Type is not (TypeCompte.Cheque or TypeCompte.Epargne))
                return Resultat.Echec("Un retrait n'est permis qu'à partir d'un compte Chèque ou Épargne.");

            if (data.Guichet.ArgentPapier < montant)
                return Resultat.Echec("Le guichet ne contient pas assez d'argent papier pour ce retrait.");

            string description;

            if (compte.Solde >= montant)
            {
                compte.Solde -= montant;
                description = $"Retrait de {montant:C} au compte {compte.Type}.";
            }
            else
            {
                // Solde insuffisant : on puise dans la marge de crédit du client.
                var marge = data.Comptes.FirstOrDefault(c => c.CodeClient == compte.CodeClient && c.Type == TypeCompte.MargeCredit);
                if (marge is null)
                    return Resultat.Echec("Solde insuffisant et aucune marge de crédit disponible pour ce client. Transaction refusée.");

                var manque = montant - compte.Solde;
                compte.Solde = 0m;
                marge.Solde += manque; // un solde positif = dette

                description = $"Retrait de {montant:C} : {manque:C} prélevés de la marge de crédit (solde insuffisant).";
            }

            data.Guichet.ArgentPapier -= montant;

            EnregistrerTransaction(data, TypeTransaction.Retrait, montant, compte.NumeroCompte,
                null, compte.CodeClient, description);

            var messageAffiche = description.Contains("marge de crédit")
                ? $"Retrait effectué. Attention : {description}"
                : $"Retrait de {montant:C} effectué avec succès.";

            return Resultat.Ok(messageAffiche);
        });
    }

    /// <summary>
    /// Transfert d'un compte Chèque vers n'importe quel autre type de compte du même client.
    /// C'est aussi la SEULE façon de rembourser une marge de crédit.
    /// </summary>
    public Resultat Transferer(string numeroCompteCheque, string numeroCompteDestination, decimal montant)
    {
        if (montant <= 0) return Resultat.Echec("Le montant doit être positif.");

        return _store.Ecrire(data =>
        {
            if (data.Guichet.EstFerme) return Resultat.Echec("Le guichet est fermé.");

            var cheque = data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompteCheque);
            if (cheque is null || cheque.Type != TypeCompte.Cheque)
                return Resultat.Echec("Le compte source d'un transfert doit être un compte Chèque.");

            var destination = data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompteDestination);
            if (destination is null) return Resultat.Echec("Compte de destination introuvable.");

            if (destination.CodeClient != cheque.CodeClient)
                return Resultat.Echec("Le compte de destination doit appartenir au même client.");

            if (cheque.Solde < montant)
                return Resultat.Echec("Solde insuffisant dans le compte Chèque pour effectuer ce transfert.");

            cheque.Solde -= montant;

            if (destination.Type == TypeCompte.MargeCredit)
            {
                // Remboursement : diminue la dette, sans jamais devenir négatif.
                destination.Solde = Math.Max(0, destination.Solde - montant);
            }
            else
            {
                destination.Solde += montant;
            }

            EnregistrerTransaction(data, TypeTransaction.Transfert, montant, cheque.NumeroCompte,
                destination.NumeroCompte, cheque.CodeClient,
                $"Transfert de {montant:C} du compte Chèque vers le compte {destination.Type}.");

            return Resultat.Ok($"Transfert de {montant:C} effectué avec succès.");
        });
    }

    public Resultat PayerFacture(string numeroCompteCheque, decimal montant)
    {
        if (montant <= 0) return Resultat.Echec("Le montant doit être positif.");

        return _store.Ecrire(data =>
        {
            if (data.Guichet.EstFerme) return Resultat.Echec("Le guichet est fermé.");

            var cheque = data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompteCheque);
            if (cheque is null || cheque.Type != TypeCompte.Cheque)
                return Resultat.Echec("Un paiement de facture doit être prélevé d'un compte Chèque.");

            var montantTotal = montant + FraisPaiementFacture;
            if (cheque.Solde < montantTotal)
                return Resultat.Echec($"Solde insuffisant (montant + frais de {FraisPaiementFacture:C}).");

            cheque.Solde -= montantTotal;

            EnregistrerTransaction(data, TypeTransaction.PaiementFacture, montant, cheque.NumeroCompte,
                null, cheque.CodeClient,
                $"Paiement de facture de {montant:C} + frais de {FraisPaiementFacture:C}.");

            return Resultat.Ok($"Paiement de facture de {montant:C} effectué (frais de {FraisPaiementFacture:C} inclus).");
        });
    }

    private static void EnregistrerTransaction(BanqueData data, TypeTransaction type, decimal montant,
        string compteSource, string? compteDestination, string codeClient, string description)
    {
        data.Transactions.Add(new Transaction
        {
            Type = type,
            Montant = montant,
            CompteSource = compteSource,
            CompteDestination = compteDestination,
            CodeClient = codeClient,
            Description = description
        });
    }
}
