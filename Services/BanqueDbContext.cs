using GuichetAutomatique.Models;
using Microsoft.EntityFrameworkCore;

namespace GuichetAutomatique.Services;

/// <summary>
/// DbContext pour la persistance des données bancaires avec Entity Framework Core.
/// </summary>
public class BanqueDbContext : DbContext
{
    public BanqueDbContext(DbContextOptions<BanqueDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<CompteBancaire> Comptes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Guichet> Guichets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration de Guichet
        modelBuilder.Entity<Guichet>().HasKey(g => g.Id);
        modelBuilder.Entity<Guichet>()
            .Property(g => g.Id)
            .ValueGeneratedNever(); // Un seul guichet, toujours Id = 1
        modelBuilder.Entity<Guichet>()
            .Property(g => g.ArgentPapier)
            .HasPrecision(18, 2);

        // Configuration de Client
        modelBuilder.Entity<Client>().HasKey(c => c.CodeClient);
        modelBuilder.Entity<Client>()
            .Property(c => c.CodeClient)
            .HasMaxLength(20);
        modelBuilder.Entity<Client>()
            .Property(c => c.Nom)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<Client>()
            .Property(c => c.Prenom)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<Client>()
            .Property(c => c.Telephone)
            .HasMaxLength(20);
        modelBuilder.Entity<Client>()
            .Property(c => c.Courriel)
            .HasMaxLength(100);
        modelBuilder.Entity<Client>()
            .Property(c => c.NipHache)
            .HasMaxLength(200)
            .IsRequired();

        // Configuration de CompteBancaire
        modelBuilder.Entity<CompteBancaire>().HasKey(c => c.NumeroCompte);
        modelBuilder.Entity<CompteBancaire>()
            .Property(c => c.NumeroCompte)
            .HasMaxLength(20);
        modelBuilder.Entity<CompteBancaire>()
            .Property(c => c.CodeClient)
            .HasMaxLength(20)
            .IsRequired();
        modelBuilder.Entity<CompteBancaire>()
            .Property(c => c.Solde)
            .HasPrecision(18, 2);

        // Clé étrangère Client -> CompteBancaire
        modelBuilder.Entity<CompteBancaire>()
            .HasOne<Client>()
            .WithMany()
            .HasForeignKey(c => c.CodeClient)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuration de Transaction
        // Volontairement sans clé étrangère : l'historique est immuable et doit survivre
        // à la suppression d'un compte, et CompteSource vaut "GUICHET" pour un ajout d'argent papier.
        modelBuilder.Entity<Transaction>().HasKey(t => t.Id);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Id)
            .HasMaxLength(36);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Montant)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.CompteSource)
            .HasMaxLength(20);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.CompteDestination)
            .HasMaxLength(20);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.CodeClient)
            .HasMaxLength(20);

        // Index pour les requêtes fréquentes
        modelBuilder.Entity<CompteBancaire>()
            .HasIndex(c => c.CodeClient);
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CodeClient);
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CompteSource);
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CompteDestination);
    }
}
