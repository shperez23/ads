using AdsManager.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class CleanupApiLogsJob
{
    private readonly IDataRetentionCleanupService _cleanupService;
    private readonly ILogger<CleanupApiLogsJob> _logger;

    public CleanupApiLogsJob(IDataRetentionCleanupService cleanupService, ILogger<CleanupApiLogsJob> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deletedRows = await _cleanupService.CleanupApiLogsAsync(cancellationToken);
        _logger.LogInformation("CleanupApiLogsJob finished. DeletedRows={DeletedRows}", deletedRows);
    }
}
