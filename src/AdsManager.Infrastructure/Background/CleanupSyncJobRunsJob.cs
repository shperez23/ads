using AdsManager.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class CleanupSyncJobRunsJob
{
    private readonly IDataRetentionCleanupService _cleanupService;
    private readonly ILogger<CleanupSyncJobRunsJob> _logger;

    public CleanupSyncJobRunsJob(IDataRetentionCleanupService cleanupService, ILogger<CleanupSyncJobRunsJob> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deletedRows = await _cleanupService.CleanupSyncJobRunsAsync(cancellationToken);
        _logger.LogInformation("CleanupSyncJobRunsJob finished. DeletedRows={DeletedRows}", deletedRows);
    }
}
