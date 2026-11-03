using System.Security.Cryptography;
using System.Text;

namespace GuichetAutomatique.Services;

/// <summary>
/// Utilitaires de sécurité : hachage NIP avec salt PBKDF2.
/// Ne pas stocker les NIP en plaintext pour des raisons évidentes.
/// </summary>
public static class SecurityHelper
{
    private const int SaltLength = 16; // bytes
    private const int HashLength = 32; // bytes (256 bits)
    private const int Iterations = 10000; // OWASP recommend minimum 10000 for PBKDF2

    /// <summary>
    /// Hache un NIP avec un salt aléatoire. Retourne une chaîne
    /// encodée en base64 contenant salt + hash.
    /// </summary>
    public static string HasherNip(string nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
            throw new ArgumentException("Le NIP ne peut pas être vide.");

        // Générer un salt aléatoire
        var salt = new byte[SaltLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hacher le NIP avec le salt
        using (var pbkdf2 = new Rfc2898DeriveBytes(nip, salt, Iterations, HashAlgorithmName.SHA256))
        {
            var hash = pbkdf2.GetBytes(HashLength);

            // Retourner salt + hash encodés en base64 pour stockage
            var combined = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, combined, salt.Length, hash.Length);

            return Convert.ToBase64String(combined);
        }
    }

    /// <summary>
    /// Vérifie qu'un NIP en plaintext correspond au hash stocké.
    /// </summary>
    public static bool VerifierNip(string nipSaisi, string nipHache)
    {
        if (string.IsNullOrWhiteSpace(nipSaisi) || string.IsNullOrWhiteSpace(nipHache))
            return false;

        try
        {
            // Décoder le hash stocké (salt + hash)
            var combined = Convert.FromBase64String(nipHache);
            if (combined.Length < SaltLength + HashLength)
                return false;

            // Extraire le salt
            var salt = new byte[SaltLength];
            Buffer.BlockCopy(combined, 0, salt, 0, SaltLength);

            // Réhacher le NIP saisi avec le salt extrait
            using (var pbkdf2 = new Rfc2898DeriveBytes(nipSaisi, salt, Iterations, HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(HashLength);

                // Comparer en temps constant pour prévenir les timing attacks
                return ConstantTimeEquals(hash, combined, SaltLength);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Comparaison en temps constant pour éviter les timing attacks.
    /// </summary>
    private static bool ConstantTimeEquals(byte[] hash, byte[] combined, int saltLength)
    {
        var result = 0;
        for (int i = 0; i < hash.Length && saltLength + i < combined.Length; i++)
        {
            result |= hash[i] ^ combined[saltLength + i];
        }
        return result == 0;
    }
}
