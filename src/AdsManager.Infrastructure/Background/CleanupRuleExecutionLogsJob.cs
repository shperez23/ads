using AdsManager.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AdsManager.Infrastructure.Background;

public sealed class CleanupRuleExecutionLogsJob
{
    private readonly IDataRetentionCleanupService _cleanupService;
    private readonly ILogger<CleanupRuleExecutionLogsJob> _logger;

    public CleanupRuleExecutionLogsJob(IDataRetentionCleanupService cleanupService, ILogger<CleanupRuleExecutionLogsJob> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deletedRows = await _cleanupService.CleanupRuleExecutionLogsAsync(cancellationToken);
        _logger.LogInformation("CleanupRuleExecutionLogsJob finished. DeletedRows={DeletedRows}", deletedRows);
    }
}
