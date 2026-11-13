# Simulateur de guichet automatique — 420-P11-ID

Application web réalisée en **C# (ASP.NET Core 8 / Razor Pages)** avec **Bootstrap 5** et
**JavaScript** pour l'interface. Les données (clients, comptes, transactions) sont lues et
écrites avec **Entity Framework Core** dans une base de données SQLite locale
(`data/banque.db`). La base et les données de démonstration sont créées automatiquement au
premier démarrage.

> Le PHP n'a pas été utilisé : il tourne sur un serveur séparé (Apache/Nginx) et ne peut pas
> partager le même processus qu'une application C#. Puisque C# était demandé, tout le back-end
> (logique d'affaires, persistance, sessions) est en C#, et Bootstrap/HTML/JS gèrent uniquement
> l'affichage — c'est l'architecture standard pour ce genre de projet .NET.

## Démarrer le projet dans Visual Studio Code

1. Installer le **SDK .NET 8** : https://dotnet.microsoft.com/download/dotnet/8.0
2. Installer l'extension VS Code **C# Dev Kit** (ou `C#` par ms-dotnettools).
3. Ouvrir le dossier `GuichetAutomatique` dans VS Code.
4. Dans un terminal, à la racine du dossier :
   ```
   dotnet restore
   dotnet run
   ```
5. Ouvrir l'URL affichée dans la console (ex. `https://localhost:5001`).

Les fournisseurs NuGet Entity Framework Core SQLite et SQL Server sont référencés dans le
projet. La configuration locale utilise SQLite; un serveur SQL Server peut être utilisé en
remplaçant le fournisseur et la chaîne de connexion.

## Comptes de démonstration

| Code client | NIP  | Comptes                                              |
|-------------|------|-------------------------------------------------------|
| 1001        | 1234 | Chèque (1 500 $), Épargne (3 200 $), Hypothécaire (150 000 $), Marge de crédit (0 $) |
| 1002        | 5678 | Chèque (400 $) seulement                              |

## Accès administrateur

Lien « Accès administrateur » sur l'écran de connexion.
**Mot de passe : `admin123`** (configurable dans `appsettings.json`, clé `Banque:AdminPassword`).

## Base de données (mapping Entity Framework Core)

Fichier SQLite : `data/banque.db` (non versionné, recréé au premier `dotnet run`).
Le mapping est défini dans `Services/BanqueDbContext.cs`.

```
Guichets (1 seule ligne)          Clients
  Id            PK (= 1)            CodeClient   PK
  ArgentPapier                      Nom, Prenom, Telephone, Courriel
  EstFerme                          NipHache     (PBKDF2 + sel)
                                    TentativesEchoueesConnexion, EstBloque
                                         │ 1
                                         │ FK, ON DELETE CASCADE
                                         │ n
Transactions                      Comptes
  Id                PK (GUID)       NumeroCompte PK
  DateHeure, Type, Montant          CodeClient   FK → Clients  (index)
  CompteSource      (index)         Type         0=Cheque 1=Epargne 2=Hypothecaire 3=MargeCredit
  CompteDestination (index)         Solde, DateCreation
  CodeClient        (index)
  Description
```

- `Transactions` n'a **pas de clé étrangère** : l'historique est immuable, doit survivre à la
  suppression d'un compte, et `CompteSource` vaut `GUICHET` pour un ajout d'argent papier.
- `Transactions.Type` : 0=Depot 1=Retrait 2=Transfert 3=PaiementFacture 4=InteretEpargne
  5=InteretMargeCredit 6=PrelevementHypothecaire 7=AjoutArgentPapier.
- La base est créée par `EnsureCreated()` (pas de migrations). Après un changement de modèle,
  supprimer `data/banque.db` pour qu'elle soit recréée.

### Consulter la base depuis le terminal

```
sqlite3 data/banque.db
sqlite> .tables
sqlite> .mode box
sqlite> SELECT * FROM Comptes;
sqlite> .quit
```

Ou dans VS Code avec l'extension **Database Client** (ouvrir `data/banque.db` en connexion SQLite).

## Structure du projet

```
Models/        Client, CompteBancaire, Transaction, Guichet, BanqueData (POCOs)
Services/
  BanqueDbContext.cs     modèle Entity Framework Core et tables de la base
  DataStore.cs           lectures/écritures EF Core avec transactions
  DonneesInitiales.cs    données de démonstration au premier lancement
  BanqueService.cs       connexion (3 essais, blocage), consultation
  BanqueService.Transactions.cs   dépôt, retrait, transfert, paiement de facture (côté client)
  BanqueService.Admin.cs          toutes les opérations administrateur
Pages/
  Index.cshtml(.cs)      écran de connexion (code client + NIP au clavier numérique)
  Client.cshtml(.cs)     tableau de bord client (soldes, 4 transactions, historique)
  Admin.cshtml(.cs)      panneau administrateur (clients, comptes, opérations, historique)
wwwroot/       CSS (thème "guichet") + JS (clavier numérique du NIP)
```

## Règles d'affaires implémentées

- Connexion : 3 tentatives de NIP max, sinon le compte est bloqué (message + contact banque).
- Comptes : Chèque obligatoire, Épargne, Hypothécaire, Marge de crédit (un seul par client).
- Dépôt : Chèque / Épargne / Hypothécaire uniquement ; n'affecte jamais l'argent papier du guichet.
- Retrait : Chèque / Épargne uniquement, multiple de 10 $, max 1 000 $/transaction ; si le
  solde du compte est insuffisant, la différence est puisée dans la marge de crédit (avec
  message d'avertissement) ; si le client n'a pas de marge de crédit, la transaction est refusée ;
  si l'argent papier du guichet est insuffisant, la transaction est aussi refusée.
- Transfert : toujours à partir du compte Chèque, vers n'importe quel autre type de compte ;
  c'est la seule façon de rembourser une marge de crédit. Si le solde du Chèque est insuffisant,
  la différence est puisée dans la marge de crédit (sauf pour rembourser la marge elle-même).
- Paiement de facture : prélevé du compte Chèque, frais fixes de 1,25 $ ; si le solde est
  insuffisant, la différence est puisée dans la marge de crédit (sinon, transaction refusée).
- Chaque transaction individuelle est sauvegardée (historique complet, jamais seulement le solde final).
- Administrateur : créer client (code de 4 chiffres) et compte (un seul Chèque et une seule
  marge de crédit par client, marge créée sans dette), bloquer/débloquer (le déblocage redonne
  3 essais de NIP), voir la liste de tous les comptes, ajouter de l'argent papier
  (max 20 000 $), fermer et rouvrir le guichet, payer 1 % d'intérêt aux comptes Épargne (solde positif), charger 5 %
  d'intérêt aux marges de crédit endettées, prélever un montant sur un compte hypothécaire
  spécifique (avec recours à la marge de crédit si le solde est insuffisant), consulter
  l'historique de toutes les transactions par compte.

## Pistes d'amélioration (hors barème strict, mais faciles à ajouter)

- Remplacer le mot de passe admin en clair (dans `appsettings.json`) par un compte administrateur en base de données, avec mot de passe haché.
- Ajouter des tests unitaires sur `BanqueService` (la logique est isolée des vues, donc testable).
- Valider davantage les entrées côté client (JavaScript) avant l'envoi du formulaire.
