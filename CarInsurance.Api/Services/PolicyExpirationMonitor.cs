using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarInsurance.Api.Services;

public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

public sealed class PolicyExpirationMonitor(
    IServiceProvider services,
    ILogger<PolicyExpirationMonitor> logger,
    IClock clock) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger<PolicyExpirationMonitor> _logger = logger;
    private readonly IClock _clock = clock;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await ProcessStartupAsync(stoppingToken); }
        catch (Exception ex) { _logger.LogError(ex, "Startup processing failed."); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessWindowAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Periodic processing failed."); }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    private static DateTimeOffset ComputeExpiryLocal(DateOnly endDate, DateTimeOffset now)
    {
        var midnight = endDate.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, now.Offset).AddDays(1);
    }

    private async Task ProcessStartupAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = _clock.Now;
        var windowStart = now.AddHours(-1);
        var today = DateOnly.FromDateTime(now.LocalDateTime.Date);

        var candidates = await db.Policies
            .Where(p => p.ExpirationLoggedAt == null && p.EndDate <= today)
            .ToListAsync(ct);

        foreach (var p in candidates)
        {
            var expiryLocal = ComputeExpiryLocal(p.EndDate, now);

            if (expiryLocal <= now && expiryLocal > windowStart)
            {
                _logger.LogInformation(
                    "Policy {PolicyId} (Car {CarId}, Provider {Provider}) expired at {Expiry} (EndDate {EndDate}).",
                    p.Id, p.CarId, p.Provider, expiryLocal, p.EndDate);

                p.ExpirationLoggedAt = now;
            }
            else if (expiryLocal < windowStart)
            {
                p.ExpirationLoggedAt = now;
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    private async Task ProcessWindowAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = _clock.Now;
        var windowStart = now.AddHours(-1);

        var minDate = DateOnly.FromDateTime(windowStart.LocalDateTime.Date).AddDays(-1);
        var maxDate = DateOnly.FromDateTime(now.LocalDateTime.Date);

        var candidates = await db.Policies
            .Where(p => p.ExpirationLoggedAt == null &&
                        p.EndDate >= minDate && p.EndDate <= maxDate)
            .ToListAsync(ct);

        foreach (var p in candidates)
        {
            var expiryLocal = ComputeExpiryLocal(p.EndDate, now);

            if (expiryLocal <= now && expiryLocal > windowStart)
            {
                _logger.LogInformation(
                    "Policy {PolicyId} (Car {CarId}, Provider {Provider}) expired at {Expiry} (EndDate {EndDate}).",
                    p.Id, p.CarId, p.Provider, expiryLocal, p.EndDate);

                p.ExpirationLoggedAt = now;
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }
}
