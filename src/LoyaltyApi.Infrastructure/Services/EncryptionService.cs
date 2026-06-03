using System.Security.Cryptography;
using System.Text;
using LoyaltyApi.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace LoyaltyApi.Infrastructure.Services;

/// <summary>
/// AES-256-GCM symmetric encryption service.
///
/// Ciphertext format (all Base64-encoded as a single string):
///   [12-byte nonce][16-byte auth tag][variable-length ciphertext]
///
/// The nonce is randomly generated per encryption call, so the same plaintext
/// produces a different ciphertext each time (non-deterministic).
/// The authentication tag provides integrity verification — tampering is detected on decrypt.
///
/// Key is read from IConfiguration["Encryption:Key"] as a Base64-encoded 32-byte value.
/// </summary>
internal sealed class EncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12;   // 96-bit nonce for GCM
    private const int TagSizeBytes = 16;      // 128-bit authentication tag

    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException(
                $"Encryption:Key must be exactly 32 bytes (256 bits). Got {_key.Length} bytes.");
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[plaintextBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Concatenate: nonce || tag || ciphertext
        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertextBase64)
    {
        ArgumentNullException.ThrowIfNull(ciphertextBase64);

        var combined = Convert.FromBase64String(ciphertextBase64);

        if (combined.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Invalid ciphertext: too short to contain nonce and tag.");

        var nonce = combined.AsSpan(0, NonceSizeBytes);
        var tag = combined.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = combined.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
