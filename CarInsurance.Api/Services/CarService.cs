using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }

    public async Task<ClaimDto> AddClaimAsync(long carId, CreateClaimRequest req)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        if (req.Amount < 0) throw new ArgumentException("Amount must be >= 0");

        var entity = new Claim
        {
            CarId = carId,
            ClaimDate = req.ClaimDate,
            Description = req.Description,
            Amount = req.Amount
        };

        _db.Add(entity);
        await _db.SaveChangesAsync();

        return new ClaimDto(
            entity.Id,
            entity.CarId,
            entity.ClaimDate.ToString("yyyy-MM-dd"),
            entity.Description,
            entity.Amount
        );
    }

    public async Task<List<CarHistoryEntry>> GetHistoryAsync(long carId)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        
        var policiesRaw = await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new { p.StartDate, p.EndDate, p.Provider })
            .ToListAsync();

        var claimsRaw = await _db.Claims
            .Where(cl => cl.CarId == carId)
            .Select(cl => new { cl.ClaimDate, cl.Description, cl.Amount })
            .ToListAsync();

        
        var policies = policiesRaw
            .Select(p => new CarHistoryEntry(
                "Policy",
                p.StartDate.ToString("yyyy-MM-dd"),
                p.EndDate.ToString("yyyy-MM-dd"),
                p.Provider,
                null
            ));

        var claims = claimsRaw
            .Select(cl => new CarHistoryEntry(
                "Claim",
                cl.ClaimDate.ToString("yyyy-MM-dd"),
                null,
                cl.Description,
                cl.Amount
            ));

        
        return policies.Concat(claims)
                       .OrderBy(e => e.StartDate, StringComparer.Ordinal)
                       .ToList();
    }
}


