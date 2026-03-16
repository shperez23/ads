using AdsManager.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence;

public sealed class AdsManagerDbContext
    : AppDbContext<AdsManagerDbContext>
{
    public AdsManagerDbContext(DbContextOptions<AdsManagerDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }
}
