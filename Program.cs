using GuichetAutomatique.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configurer la culture pour afficher les montants en dollars canadiens (fr-CA)
var cultureInfo = new CultureInfo("fr-CA");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configurer Entity Framework Core avec SQLite pour la démonstration locale
var connectionString = builder.Configuration.GetConnectionString("BanqueConnection");
builder.Services.AddDbContext<BanqueDbContext>(options =>
    options.UseSqlite(connectionString)
);

builder.Services.AddRazorPages();
builder.Services.AddScoped<DataStore>();
builder.Services.AddScoped<BanqueService>();
builder.Services.AddSingleton<AuditLogger>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Appliquer la création de base de données à la première exécution
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BanqueDbContext>();
        db.Database.EnsureCreated();
        
        if (!db.Clients.Any())
        {
            var donnees = DonneesInitiales.Creer();
            db.Guichets.Add(donnees.Guichet);
            db.Clients.AddRange(donnees.Clients);
            db.Comptes.AddRange(donnees.Comptes);
            db.Transactions.AddRange(donnees.Transactions);
            db.SaveChanges();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Erreur lors de l'initialisation de la base de données: {ex.Message}");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Erreur");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
