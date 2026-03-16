using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Rules;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;

namespace AdsManager.Application.Services;

public sealed class RuleService : IRuleService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ITenantProvider _tenantProvider;

    public RuleService(IRuleRepository ruleRepository, ITenantProvider tenantProvider)
    {
        _ruleRepository = ruleRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyCollection<RuleDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<IReadOnlyCollection<RuleDto>>.Fail("Tenant no resuelto");

        var rules = await _ruleRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<RuleDto>>.Ok(rules.Select(Map).ToArray());
    }

    public async Task<Result<RuleDto>> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<RuleDto>.Fail("Tenant no resuelto");

        var rule = new Rule
        {
            TenantId = tenantId,
            Name = request.Name,
            EntityLevel = request.EntityLevel,
            Metric = request.Metric,
            Operator = request.Operator,
            Threshold = request.Threshold,
            Action = request.Action,
            IsActive = request.IsActive
        };

        await _ruleRepository.AddAsync(rule, cancellationToken);
        return Result<RuleDto>.Ok(Map(rule), "Regla creada correctamente");
    }

    public async Task<Result<RuleDto>> UpdateAsync(Guid ruleId, UpdateRuleRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<RuleDto>.Fail("Tenant no resuelto");

        var rule = await _ruleRepository.GetByIdAsync(tenantId, ruleId, cancellationToken);
        if (rule is null)
            return Result<RuleDto>.Fail("Regla no encontrada");

        rule.Name = request.Name;
        rule.EntityLevel = request.EntityLevel;
        rule.Metric = request.Metric;
        rule.Operator = request.Operator;
        rule.Threshold = request.Threshold;
        rule.Action = request.Action;
        rule.IsActive = request.IsActive;

        await _ruleRepository.UpdateAsync(rule, cancellationToken);
        return Result<RuleDto>.Ok(Map(rule), "Regla actualizada correctamente");
    }

    public Task<Result<RuleDto>> ActivateAsync(Guid ruleId, CancellationToken cancellationToken = default)
        => UpdateActiveStatusAsync(ruleId, true, cancellationToken);

    public Task<Result<RuleDto>> DeactivateAsync(Guid ruleId, CancellationToken cancellationToken = default)
        => UpdateActiveStatusAsync(ruleId, false, cancellationToken);

    private async Task<Result<RuleDto>> UpdateActiveStatusAsync(Guid ruleId, bool isActive, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Result<RuleDto>.Fail("Tenant no resuelto");

        var rule = await _ruleRepository.GetByIdAsync(tenantId, ruleId, cancellationToken);
        if (rule is null)
            return Result<RuleDto>.Fail("Regla no encontrada");

        rule.IsActive = isActive;
        await _ruleRepository.UpdateAsync(rule, cancellationToken);

        return Result<RuleDto>.Ok(Map(rule), isActive ? "Regla activada" : "Regla desactivada");
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        tenantId = _tenantProvider.GetTenantId() ?? Guid.Empty;
        return tenantId != Guid.Empty;
    }

    private static RuleDto Map(Rule rule)
        => new(rule.Id, rule.Name, rule.EntityLevel, rule.Metric, rule.Operator, rule.Threshold, rule.Action, rule.IsActive, rule.CreatedAt, rule.UpdatedAt);
}
