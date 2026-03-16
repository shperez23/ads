using AdsManager.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class CleanupAuditLogsJob
{
    private readonly IDataRetentionCleanupService _cleanupService;
    private readonly ILogger<CleanupAuditLogsJob> _logger;

    public CleanupAuditLogsJob(IDataRetentionCleanupService cleanupService, ILogger<CleanupAuditLogsJob> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deletedRows = await _cleanupService.CleanupAuditLogsAsync(cancellationToken);
        _logger.LogInformation("CleanupAuditLogsJob finished. DeletedRows={DeletedRows}", deletedRows);
    }
}
