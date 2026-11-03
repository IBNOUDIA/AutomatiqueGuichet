using GuichetAutomatique.Models;

namespace GuichetAutomatique.Services;

/// <summary>
/// Toutes les opérations réservées à l'ADMINISTRATEUR (voir spécifications, page 4).
/// </summary>
public partial class BanqueService
{
    public Resultat CreerClient(string codeClient, string nom, string prenom, string telephone,
        string courriel, string nip, decimal soldeInitialCheque)
    {
        if (string.IsNullOrWhiteSpace(codeClient)) return Resultat.Echec("Le code client est requis.");
        if (nip.Length != 4 || !nip.All(char.IsDigit)) return Resultat.Echec("Le NIP doit être composé de 4 chiffres.");

        return _store.Ecrire(data =>
        {
            if (data.Clients.Any(c => c.CodeClient == codeClient))
                return Resultat.Echec("Ce code client existe déjà.");

            data.Clients.Add(new Client
            {
                CodeClient = codeClient,
                Nom = nom,
                Prenom = prenom,
                Telephone = telephone,
                Courriel = courriel,
                NipHache = SecurityHelper.HasherNip(nip),
                TentativesEchoueesConnexion = 0
            });

            // Un compte Chèque est obligatoire : on le crée automatiquement avec le nouveau client.
            data.Comptes.Add(new CompteBancaire
            {
                CodeClient = codeClient,
                Type = TypeCompte.Cheque,
                Solde = soldeInitialCheque
            });

            return Resultat.Ok($"Client {prenom} {nom} créé avec succès (compte Chèque inclus).");
        });
    }

    public Resultat CreerCompte(string codeClient, TypeCompte type, decimal soldeInitial)
    {
        return _store.Ecrire(data =>
        {
            var client = data.Clients.FirstOrDefault(c => c.CodeClient == codeClient);
            if (client is null) return Resultat.Echec("Client introuvable.");

            var comptesClient = data.Comptes.Where(c => c.CodeClient == codeClient).ToList();

            if (!comptesClient.Any(c => c.Type == TypeCompte.Cheque) && type != TypeCompte.Cheque)
                return Resultat.Echec("Le client doit d'abord posséder un compte Chèque.");

            if (type == TypeCompte.MargeCredit && comptesClient.Any(c => c.Type == TypeCompte.MargeCredit))
                return Resultat.Echec("Ce client possède déjà une marge de crédit (un seul compte de ce type est permis).");

            data.Comptes.Add(new CompteBancaire
            {
                CodeClient = codeClient,
                Type = type,
                Solde = soldeInitial
            });

            return Resultat.Ok($"Compte {type} créé avec succès pour le client {codeClient}.");
        });
    }

    public Resultat BasculerBlocageClient(string codeClient, bool bloquer)
    {
        return _store.Ecrire(data =>
        {
            var client = data.Clients.FirstOrDefault(c => c.CodeClient == codeClient);
            if (client is null) return Resultat.Echec("Client introuvable.");

            client.EstBloque = bloquer;
            TentativesEchouees.Remove(codeClient);

            return Resultat.Ok(bloquer
                ? $"L'accès du client {codeClient} a été bloqué."
                : $"L'accès du client {codeClient} a été débloqué.");
        });
    }

    public Resultat AjouterArgentPapier(decimal montant)
    {
        if (montant <= 0) return Resultat.Echec("Le montant doit être positif.");

        return _store.Ecrire(data =>
        {
            var nouveauTotal = data.Guichet.ArgentPapier + montant;
            if (nouveauTotal > Guichet.MaximumArgentPapier)
                return Resultat.Echec($"Le guichet ne peut contenir plus de {Guichet.MaximumArgentPapier:C} (raisons de sécurité).");

            data.Guichet.ArgentPapier = nouveauTotal;

            EnregistrerTransaction(data, TypeTransaction.AjoutArgentPapier, montant, "GUICHET",
                null, "ADMIN", $"Ajout de {montant:C} d'argent papier dans le guichet.");

            return Resultat.Ok($"{montant:C} ajoutés. Le guichet contient maintenant {data.Guichet.ArgentPapier:C}.");
        });
    }

    public Resultat ModifierClient(string codeClient, string nom, string prenom, string telephone, string courriel)
    {
        if (string.IsNullOrWhiteSpace(codeClient)) return Resultat.Echec("Le code client est requis.");

        return _store.Ecrire(data =>
        {
            var client = data.Clients.FirstOrDefault(c => c.CodeClient == codeClient);
            if (client is null) return Resultat.Echec("Client introuvable.");

            client.Nom = nom;
            client.Prenom = prenom;
            client.Telephone = telephone;
            client.Courriel = courriel;

            return Resultat.Ok($"Les informations du client {codeClient} ont été mises à jour avec succès.");
        });
    }

    public Resultat FermerGuichet()
    {
        return _store.Ecrire(data =>
        {
            data.Guichet.EstFerme = true;
            return Resultat.Ok("Le guichet a été fermé. Le programme n'accepte plus de transactions.");
        });
    }

    /// <summary>Paye 1% d'intérêt à tous les comptes Épargne, en un seul clic.</summary>
    public Resultat PayerInteretEpargne()
    {
        return _store.Ecrire(data =>
        {
            var comptesEpargne = data.Comptes.Where(c => c.Type == TypeCompte.Epargne).ToList();
            foreach (var compte in comptesEpargne)
            {
                var interet = Math.Round(compte.Solde * TauxInteretEpargne, 2);
                compte.Solde += interet;
                EnregistrerTransaction(data, TypeTransaction.InteretEpargne, interet, compte.NumeroCompte,
                    null, compte.CodeClient, $"Intérêt de {TauxInteretEpargne:P0} versé sur le compte Épargne.");
            }

            return Resultat.Ok($"Intérêt de {TauxInteretEpargne:P0} versé à {comptesEpargne.Count} compte(s) Épargne.");
        });
    }

    /// <summary>Augmente de 5% le solde (la dette) de toutes les marges de crédit, en un seul clic.</summary>
    public Resultat ChargerInteretMargeCredit()
    {
        return _store.Ecrire(data =>
        {
            var margesCredit = data.Comptes.Where(c => c.Type == TypeCompte.MargeCredit && c.Solde > 0).ToList();
            foreach (var compte in margesCredit)
            {
                var interet = Math.Round(compte.Solde * TauxInteretMargeCredit, 2);
                compte.Solde += interet;
                EnregistrerTransaction(data, TypeTransaction.InteretMargeCredit, interet, compte.NumeroCompte,
                    null, compte.CodeClient, $"Intérêt de {TauxInteretMargeCredit:P0} chargé sur la marge de crédit.");
            }

            return Resultat.Ok($"Intérêt de {TauxInteretMargeCredit:P0} chargé sur {margesCredit.Count} marge(s) de crédit endettée(s).");
        });
    }

    /// <summary>
    /// Prélève un montant du choix de l'administrateur sur un compte Hypothécaire spécifique.
    /// Si le solde est insuffisant, la différence est prélevée de la marge de crédit du client (si elle existe).
    /// </summary>
    public Resultat PreleverHypothecaire(string numeroCompteHypothecaire, decimal montant)
    {
        if (montant <= 0) return Resultat.Echec("Le montant doit être positif.");

        return _store.Ecrire(data =>
        {
            var compte = data.Comptes.FirstOrDefault(c => c.NumeroCompte == numeroCompteHypothecaire);
            if (compte is null || compte.Type != TypeCompte.Hypothecaire)
                return Resultat.Echec("Compte hypothécaire introuvable.");

            string description;

            if (compte.Solde >= montant)
            {
                compte.Solde -= montant;
                description = $"Prélèvement administratif de {montant:C} sur le compte Hypothécaire.";
            }
            else
            {
                var marge = data.Comptes.FirstOrDefault(c => c.CodeClient == compte.CodeClient && c.Type == TypeCompte.MargeCredit);
                if (marge is null)
                    return Resultat.Echec("Solde hypothécaire insuffisant et aucune marge de crédit disponible. Transaction refusée.");

                var manque = montant - compte.Solde;
                compte.Solde = 0m;
                marge.Solde += manque;
                description = $"Prélèvement administratif de {montant:C} : {manque:C} prélevés de la marge de crédit (solde insuffisant).";
            }

            EnregistrerTransaction(data, TypeTransaction.PrelevementHypothecaire, montant, compte.NumeroCompte,
                null, compte.CodeClient, description);

            return Resultat.Ok(description);
        });
    }
}
