namespace AdsManager.Application.DTOs.AdAccounts;

public sealed record AdAccountDto(
    Guid Id,
    string MetaAccountId,
    string Name,
    string Currency,
    string TimezoneName,
    string Status);
