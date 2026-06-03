namespace LoyaltyApi.Application.Interfaces.Services;

public interface IEncryptionService
{
    /// <summary>Encrypts plaintext using AES-256-GCM. Returns a base64-encoded ciphertext.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a base64-encoded AES-256-GCM ciphertext produced by <see cref="Encrypt"/>.</summary>
    string Decrypt(string ciphertext);
}
