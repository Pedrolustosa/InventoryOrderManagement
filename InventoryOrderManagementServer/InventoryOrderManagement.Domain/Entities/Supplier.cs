using System.Net.Mail;
using System.Text;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Entities;

/// <summary>
/// Aggregate root representing a merchandise supplier.
/// Prepared to be referenced by future StockMovement entries.
/// </summary>
public class Supplier
{
    public const int NameMaxLength = 150;
    public const int ContactNameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 30;
    public const int NotesMaxLength = 500;
    public const int TaxIdentifierMaxLength = 50;

    private Supplier()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? TaxIdentifier { get; private set; }

    public string? NormalizedTaxIdentifier { get; private set; }

    public string? ContactName { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static Supplier Create(
        string name,
        string? taxIdentifier,
        string? contactName,
        string? email,
        string? phone,
        string? notes)
    {
        var (displayTaxIdentifier, normalizedTaxIdentifier) = NormalizeTaxIdentifier(taxIdentifier);

        return new Supplier
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            TaxIdentifier = displayTaxIdentifier,
            NormalizedTaxIdentifier = normalizedTaxIdentifier,
            ContactName = NormalizeOptionalText(contactName, ContactNameMaxLength, "Contact name"),
            Email = NormalizeEmail(email),
            Phone = NormalizeOptionalText(phone, PhoneMaxLength, "Phone"),
            Notes = NormalizeOptionalText(notes, NotesMaxLength, "Notes"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    public void Update(
        string name,
        string? taxIdentifier,
        string? contactName,
        string? email,
        string? phone,
        string? notes)
    {
        var (displayTaxIdentifier, normalizedTaxIdentifier) = NormalizeTaxIdentifier(taxIdentifier);

        Name = NormalizeName(name);
        TaxIdentifier = displayTaxIdentifier;
        NormalizedTaxIdentifier = normalizedTaxIdentifier;
        ContactName = NormalizeOptionalText(contactName, ContactNameMaxLength, "Contact name");
        Email = NormalizeEmail(email);
        Phone = NormalizeOptionalText(phone, PhoneMaxLength, "Phone");
        Notes = NormalizeOptionalText(notes, NotesMaxLength, "Notes");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Supplier is already inactive.");
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static string? NormalizeTaxIdentifierForComparison(string? taxIdentifier)
    {
        return NormalizeTaxIdentifier(taxIdentifier).Normalized;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var displayName = name.Trim();

        if (displayName.Length > NameMaxLength)
        {
            throw new DomainException($"Name cannot exceed {NameMaxLength} characters.");
        }

        return displayName;
    }

    private static (string? Display, string? Normalized) NormalizeTaxIdentifier(string? taxIdentifier)
    {
        if (string.IsNullOrWhiteSpace(taxIdentifier))
        {
            return (null, null);
        }

        var display = taxIdentifier.Trim();

        if (display.Length > TaxIdentifierMaxLength)
        {
            throw new DomainException(
                $"Tax identifier cannot exceed {TaxIdentifierMaxLength} characters.");
        }

        var builder = new StringBuilder(display.Length);
        foreach (var character in display)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        var normalized = builder.ToString();
        if (normalized.Length == 0)
        {
            return (null, null);
        }

        return (display, normalized);
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim();

        if (normalized.Length > EmailMaxLength)
        {
            throw new DomainException($"Email cannot exceed {EmailMaxLength} characters.");
        }

        try
        {
            _ = new MailAddress(normalized);
        }
        catch (FormatException)
        {
            throw new DomainException("Email format is invalid.");
        }

        return normalized;
    }
}
