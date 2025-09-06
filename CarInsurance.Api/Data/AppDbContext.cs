using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<InsurancePolicy> Policies => Set<InsurancePolicy>();
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>()
            .HasIndex(c => c.Vin)
            .IsUnique(true); // TODO: set true and handle conflicts

        modelBuilder.Entity<InsurancePolicy>()
            .Property(p => p.StartDate)
            .IsRequired(true);  
            
        modelBuilder.Entity<InsurancePolicy>()
            .Property(p => p.EndDate)
            .IsRequired(true);
                

        // EndDate intentionally left nullable for a later task
    }
}

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Owners.Any()) return;

        var ana = new Owner { Name = "Ana Pop", Email = "ana.pop@example.com" };
        var bogdan = new Owner { Name = "Bogdan Ionescu", Email = "bogdan.ionescu@example.com" };
        db.Owners.AddRange(ana, bogdan);
        db.SaveChanges();

        var car1 = new Car { Vin = "VIN12345", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = ana.Id };
        var car2 = new Car { Vin = "VIN67890", Make = "VW", Model = "Golf", YearOfManufacture = 2021, OwnerId = bogdan.Id };
        db.Cars.AddRange(car1, car2);
        db.SaveChanges();

        db.Policies.AddRange(
            new InsurancePolicy { CarId = car1.Id, Provider = "Allianz", StartDate = new DateOnly(2024,1,1), EndDate = new DateOnly(2024,12,31) },
            new InsurancePolicy { CarId = car1.Id, Provider = "Groupama", StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,12,31) }, // open-ended on purpose
            new InsurancePolicy { CarId = car2.Id, Provider = "Allianz", StartDate = new DateOnly(2025,3,1), EndDate = new DateOnly(2025,9,30) }
        );

        db.Claims.AddRange(
            new Claim { CarId = car1.Id, ClaimDate = new DateOnly(2024, 5, 12), Description = "bara spate", Amount = 1200m },
            new Claim { CarId = car1.Id, ClaimDate = new DateOnly(2025, 3, 20), Description = "geam spart", Amount = 500m },
            new Claim { CarId = car2.Id, ClaimDate = new DateOnly(2025, 4, 10), Description = "zgarietura portiera", Amount = 200m }
        );
        db.SaveChanges();
    }
}
