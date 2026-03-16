using AdsManager.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AdsManager.Infrastructure.Security;

public sealed class SecretEncryptionService : ISecretEncryptionService
{
    private readonly IDataProtector _protector;

    public SecretEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("AdsManager.MetaConnections.Secrets.v1");
    }

    public string Encrypt(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : _protector.Protect(value);

    public string Decrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            return value;
        }
    }
}
