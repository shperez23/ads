using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence;

public sealed class AdsManagerDbContext : AppDbContext
{
    public AdsManagerDbContext(DbContextOptions<AdsManagerDbContext> options) : base(options)
    {
    }
}
