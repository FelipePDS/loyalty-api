using System.Text.RegularExpressions;

namespace LoyaltyApi.Domain.ValueObjects;

/// <summary>
/// Immutable value object wrapping a validated, normalized email address.
/// Stored in plain text in the domain; encrypted at rest by the Infrastructure persistence layer.
/// </summary>
public sealed class Email : IEquatable<Email>
{
    // RFC 5321 / HTML5 email pattern — compiled once, with a 1-second timeout to prevent ReDoS.
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Creates a validated, lower-cased Email value object.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value is null, empty, or not a valid email format.</exception>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        var trimmed = value.Trim();

        if (!EmailRegex.IsMatch(trimmed))
            throw new ArgumentException($"'{trimmed}' is not a valid email address.", nameof(value));

        return new Email(trimmed.ToLowerInvariant());
    }

    public static implicit operator string(Email email) => email.Value;

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Email other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}
