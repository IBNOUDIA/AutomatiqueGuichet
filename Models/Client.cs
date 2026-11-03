namespace GuichetAutomatique.Models;

public class Client
{
    /// <summary>Clé primaire — unique, saisi à l'écran de connexion.</summary>
    public string CodeClient { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Courriel { get; set; } = string.Empty;

    /// <summary>NIP secret à quatre chiffres, haché avec salt PBKDF2.</summary>
    public string NipHache { get; set; } = string.Empty;

    /// <summary>
    /// Nombre d'essais échoués de connexion (non réinitialisé au redémarrage,
    /// mais stocké dans BanqueData pour persistance).
    /// </summary>
    public int TentativesEchoueesConnexion { get; set; } = 0;

    /// <summary>
    /// Vrai après 3 tentatives infructueuses (ou bloqué manuellement par l'administrateur).
    /// Empêche toute connexion tant qu'un administrateur ne débloque pas le compte.
    /// </summary>
    public bool EstBloque { get; set; }

    public string NomComplet => $"{Prenom} {Nom}";
}
