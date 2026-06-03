namespace LoyaltyApi.Domain.ValueObjects;

/// <summary>
/// Immutable value object wrapping a CPF document number (digits only).
/// Stored in plain text in the domain; encrypted at rest by the Infrastructure persistence layer.
/// Call <see cref="IsValid"/> before accepting the value in a registration flow.
/// </summary>
public sealed class Document : IEquatable<Document>
{
    public string Value { get; }

    private Document(string value) => Value = value;

    /// <summary>
    /// Creates a Document from a raw CPF string, stripping non-digit characters.
    /// Does NOT validate the CPF algorithm — call <see cref="IsValid"/> for that.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value contains no digits.</exception>
    public static Document Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Document cannot be empty.", nameof(value));

        var digitsOnly = new string(value.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length == 0)
            throw new ArgumentException("Document must contain at least one numeric character.", nameof(value));

        return new Document(digitsOnly);
    }

    /// <summary>
    /// Validates the stored value as a Brazilian CPF using the two-digit verification algorithm.
    /// Returns <c>false</c> for sequences of identical digits (e.g., "00000000000").
    /// </summary>
    public bool IsValid()
    {
        if (Value.Length != 11)
            return false;

        // Reject all-same-digit sequences which are syntactically valid but legally invalid.
        if (Value.Distinct().Count() == 1)
            return false;

        // First verification digit
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (Value[i] - '0') * (10 - i);

        int remainder = sum % 11;
        int firstDigit = remainder < 2 ? 0 : 11 - remainder;

        if (Value[9] - '0' != firstDigit)
            return false;

        // Second verification digit
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += (Value[i] - '0') * (11 - i);

        remainder = sum % 11;
        int secondDigit = remainder < 2 ? 0 : 11 - remainder;

        return Value[10] - '0' == secondDigit;
    }

    public bool Equals(Document? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Document other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
