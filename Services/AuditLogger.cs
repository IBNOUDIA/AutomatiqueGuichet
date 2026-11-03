namespace GuichetAutomatique.Services;

/// <summary>
/// Enregistre les événements d'audit importants (tentatives connexion, opérations admin, etc.)
/// pour des raisons de sécurité et de conformité.
/// </summary>
public class AuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly string _cheminAudit;

    public AuditLogger(ILogger<AuditLogger> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        var dossierData = Path.Combine(env.ContentRootPath, "data");
        Directory.CreateDirectory(dossierData);
        _cheminAudit = Path.Combine(dossierData, "audit.log");
    }

    /// <summary>Enregistre un événement d'audit dans le fichier de log et dans ILogger.</summary>
    public void EnregistrerEvenement(string typeEvenement, string codeClient, string details)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] {typeEvenement} | Client: {codeClient ?? "N/A"} | Détails: {details}";

        // Log dans ILogger (visible dans la console/debugger)
        _logger.LogInformation(logEntry);

        // Enregistrer dans le fichier audit.log
        try
        {
            lock (this)
            {
                File.AppendAllText(_cheminAudit, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de l'enregistrement de l'audit: {ex.Message}");
        }
    }

    public void TentativeConnexionEchouee(string codeClient, int essaisRestants) =>
        EnregistrerEvenement("FAILED_LOGIN", codeClient, $"Essais restants: {essaisRestants}");

    public void CompteBloque(string codeClient) =>
        EnregistrerEvenement("ACCOUNT_LOCKED", codeClient, "Compte bloqué suite à 3 tentatives échouées");

    public void ConnexionReussie(string codeClient) =>
        EnregistrerEvenement("LOGIN_SUCCESS", codeClient, "Connexion réussie");

    public void TentativeAccesAdminEchouee() =>
        EnregistrerEvenement("ADMIN_LOGIN_FAILED", string.Empty, "Mot de passe admin incorrect");

    public void AccesAdminReussi() =>
        EnregistrerEvenement("ADMIN_LOGIN_SUCCESS", string.Empty, "Admin connecté");

    public void OperationAdmin(string operation, string details) =>
        EnregistrerEvenement("ADMIN_OPERATION", null, $"{operation}: {details}");
}
