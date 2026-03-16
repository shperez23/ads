using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Rules;

namespace AdsManager.Application.Interfaces.Services;

public interface IRuleService
{
    Task<Result<IReadOnlyCollection<RuleDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<RuleDto>> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RuleDto>> UpdateAsync(Guid ruleId, UpdateRuleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RuleDto>> ActivateAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<Result<RuleDto>> DeactivateAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
