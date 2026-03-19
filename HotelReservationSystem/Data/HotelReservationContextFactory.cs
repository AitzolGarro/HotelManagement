using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HotelReservationSystem.Data;

/// <summary>
/// Fábrica de contexto en tiempo de diseño para que las herramientas de EF Core
/// puedan crear el DbContext sin necesidad de iniciar la aplicación completa.
/// Se utiliza al ejecutar comandos como 'dotnet ef migrations add'.
/// </summary>
public class HotelReservationContextFactory : IDesignTimeDbContextFactory<HotelReservationContext>
{
    public HotelReservationContext CreateDbContext(string[] args)
    {
        // Cargar configuración desde appsettings.json
        // El directorio base puede ser la raíz del proyecto o el directorio del proyecto
        var basePath = Directory.GetCurrentDirectory();
        var projectDir = Path.Combine(basePath, "HotelReservationSystem");
        if (!File.Exists(Path.Combine(projectDir, "appsettings.json")))
        {
            projectDir = basePath;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDir)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<HotelReservationContext>();

        var useSqlite = configuration.GetValue<bool>("UseSqlite");

        if (useSqlite)
        {
            // Usar SQLite para desarrollo y demo
            var sqliteConnection = configuration.GetConnectionString("SqliteConnection")
                ?? "Data Source=./hotel_reservation_demo.db";
            optionsBuilder.UseSqlite(sqliteConnection);
        }
        else
        {
            // Usar SQL Server para producción
            var sqlServerConnection = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(sqlServerConnection);
        }

        return new HotelReservationContext(optionsBuilder.Options);
    }
}
