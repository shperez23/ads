namespace AdsManager.Application.Interfaces;

public interface ISecretEncryptionService
{
    string Encrypt(string value);
    string Decrypt(string value);
}
