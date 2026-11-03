# 📚 Guide Complet du Guichet Automatique

## Table des Matières
1. [Installation](#installation)
2. [Fonctionnalités](#fonctionnalités)
3. [Structure du Code](#structure-du-code)
4. [Utilisation](#utilisation)
5. [Données de Test](#données-de-test)
6. [Architecture](#architecture)
7. [Évolutions Futures](#évolutions-futures)

---

## Installation

### Prérequis
- **Windows/Mac/Linux**
- **.NET SDK 6.0+** ([Télécharger](https://dotnet.microsoft.com/download))
- **Visual Studio Code** (optionnel, recommandé)
- **Git**

### Étapes

```bash
# 1. Cloner le dépôt
git clone https://github.com/IBNOUDIA/AutomatiqueGuichet.git
cd AutomatiqueGuichet

# 2. Restaurer les dépendances
dotnet restore

# 3. Compiler le projet
dotnet build

# 4. Exécuter l'application
dotnet run --project src/AutomatiqueGuichet.csproj
```

---

## Fonctionnalités

### 🔐 Authentification
- Connexion par numéro de compte + PIN
- PIN masqué lors de la saisie (affichage de *)
- Protection contre les accès non autorisés
- Blocage après 3 tentatives échouées
- Déverrouillage par administrateur

### 💰 Opérations Bancaires

#### Consultation de Solde
```
Affiche le solde actuel du compte connecté
```

#### Retrait
```
- Vérification de la disponibilité des fonds
- Limitation au solde disponible
- Enregistrement dans l'historique
```

#### Dépôt
```
- Augmentation du solde
- Montants positifs uniquement
- Enregistrement immédiat
```

#### Virement
```
- Transfert vers un autre compte
- Frais de 1% automatiquement appliqués
- Vérification du compte destinataire
- Enregistrement bilatéral
```

### 📋 Historique des Transactions
```
- Date et heure précises
- Type d'opération (Retrait, Dépôt, Virement)
- Montant transféré
- Solde après l'opération
- Description détaillée
```

### 🔑 Gestion du PIN
```
- Changement du code d'accès
- Vérification de l'ancien PIN
- Confirmation du nouveau PIN
- Validation de la correspondance
```

---

## Structure du Code

```
AutomatiqueGuichet/
│
├── src/
│   ├── Models/
│   │   ├── Account.cs          # Classe représentant un compte
│   │   └── Transaction.cs      # Classe représentant une transaction
│   │
│   ├── Services/
│   │   └── ATMService.cs       # Logique métier du guichet
│   │
│   ├── Program.cs              # Interface utilisateur (UI)
│   └── AutomatiqueGuichet.csproj
│
├── .github/workflows/
│   └── ci.yml                  # Configuration CI/CD
│
├── README.md                   # Documentation principale
├── GUIDE.md                    # Ce fichier
├── LICENSE                     # Licence MIT
└── .gitignore                  # Fichiers ignorés par Git
```

### Détail des Classes

#### 📦 Account.cs
Gère un compte bancaire individuel avec :
- Numéro de compte
- Solde et PIN
- Historique des transactions
- Méthodes : `VerifyPIN()`, `Withdraw()`, `Deposit()`, `Transfer()`, `ChangePIN()`

#### 📦 Transaction.cs
Enregistre chaque opération avec :
- Type (Retrait/Dépôt/Virement)
- Montant et solde après
- Timestamp et description

#### 📦 ATMService.cs
Orchestre la logique du guichet :
- Gestion des authentifications
- Coordination des opérations
- Gestion des erreurs
- Initialisation des comptes

#### 📦 Program.cs
Interface utilisateur (console) :
- Menu principal et sous-menus
- Formatage des écrans
- Interaction avec l'utilisateur
- Boucles de contrôle

---

## Utilisation

### Scénario 1 : Se Connecter

```
Choisissez une option: 1

Entrez votre numéro de compte: ACC001
Entrez votre PIN: 1234

✅ Connexion réussie! Bienvenue Ahmed Ben
```

### Scénario 2 : Retirer de l'Argent

```
Choisissez une option: 2

Montant à retirer: 100

✅ Retrait réussi! Montant: 100,00 €
💰 Solde restant: 900,00 €
```

### Scénario 3 : Effectuer un Virement

```
Choisissez une option: 4

Numéro de compte destinataire: ACC002
Montant à virer: 50

✅ Virement réussi!
   Montant: 50,00 €
   Frais (1%): 0,50 €
   Nouveau solde: 849,50 €
```

### Scénario 4 : Consulter l'Historique

```
Choisissez une option: 5

[2024-01-15 10:30:45] Retrait          | Montant:     100,00 € | Solde:     900,00 € | Retrait
[2024-01-15 10:31:12] Virement Émis    | Montant:      50,00 € | Solde:     849,50 € | Virement vers ACC002
[2024-01-15 10:32:00] Dépôt            | Montant:     200,00 € | Solde:   1 049,50 € | Dépôt
```

---

## Données de Test

### Comptes Disponibles

| Compte | PIN  | Solde | Titulaire      |
|--------|------|-------|----------------|
| ACC001 | 1234 | 1000€ | Ahmed Ben      |
| ACC002 | 5678 | 500€  | Fatima Sidi    |
| ACC003 | 9012 | 2000€ | Mohamed Ali    |
| ACC004 | 3456 | 1500€ | Leila Zahra    |

### Tester le Blocage
```
Essayez de vous connecter 3 fois avec un mauvais PIN :
- 1ère tentative échouée
- 2ème tentative échouée
- 3ème tentative échouée → Compte bloqué
```

---

## Architecture

### Flux d'Authentification
```
┌─────────────────┐
│  Se Connecter   │
└────────┬────────┘
         │
         ▼
┌──────────────────────┐
│ Vérifier le Compte   │
└────────┬─────────────┘
         │
    ┌────┴────┐
    │          │
   Oui        Non
    │          │
    ▼          ▼
┌──────────┐  ❌
│Vérifier  │  Compte
│  PIN     │  inexistant
└────┬─────┘
     │
 ┌───┴───┐
 │       │
Oui     Non
 │       │
 ▼       ▼
✅      ❌ + Tentative++
```

### Flux d'une Transaction
```
┌─────────────────┐
│ Retrait/Dépôt   │
└────────┬────────┘
         │
         ▼
┌─────────────────────┐
│ Vérifier Solde      │
│ Montant Valide      │
└────────┬────────────┘
         │
    ┌────┴────┐
   Oui       Non
    │         │
    ▼         ▼
┌─────────┐  ❌
│Modifier │ Erreur
│Solde    │
└────┬────┘
     │
     ▼
┌──────────────────┐
│ Enregistrer      │
│ Transaction      │
└──────────────────┘
```

---

## Évolutions Futures

### Court Terme
- [ ] Sauvegarde persistante (base de données)
- [ ] Authentification à deux facteurs (2FA)
- [ ] Gestion des chèques
- [ ] Paiement de factures

### Moyen Terme
- [ ] Interface graphique (WPF/MAUI)
- [ ] API REST pour intégration
- [ ] Système de notifications
- [ ] Rapport d'activité détaillé

### Long Terme
- [ ] Application mobile (Android/iOS)
- [ ] Biométrie (empreinte digitale)
- [ ] Cryptographie avancée
- [ ] Système de recommandations financières

---

## Dépannage

### ❌ "Aucun compte authentifié"
```
✓ Solution : Connectez-vous d'abord avec "Se connecter"
```

### ❌ "Compte bloqué après 3 tentatives"
```
✓ Solution : Contactez l'administrateur pour déverrouiller
```

### ❌ "Montant invalide"
```
✓ Solution : Entrez un nombre positif (ex. : 100, 50.50)
```

### ❌ "Solde insuffisant"
```
✓ Solution : Le montant dépasse votre solde disponible
```

---

## Support

📧 **Email** : support@ibnoudia.dev  
🐛 **Issues** : [GitHub Issues](https://github.com/IBNOUDIA/AutomatiqueGuichet/issues)  
💬 **Discussions** : [GitHub Discussions](https://github.com/IBNOUDIA/AutomatiqueGuichet/discussions)

---

## License

Ce projet est sous licence **MIT** - Libre d'utilisation, de modification et de distribution.

**© 2024 IBNOUDIA** - Tous droits réservés.
