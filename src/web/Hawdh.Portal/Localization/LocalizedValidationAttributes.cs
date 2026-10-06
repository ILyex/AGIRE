using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Hawdh.Portal.Localization;

public sealed class LocalizedRequiredAttribute : RequiredAttribute
{
    public override string FormatErrorMessage(string name) => IsFrench
        ? "Ce champ est obligatoire."
        : "هذا الحقل مطلوب.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalizedEmailAddressAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public override bool IsValid(object? value) =>
        value is null || value is string address && (string.IsNullOrWhiteSpace(address) || EmailValidator.IsValid(address));

    public override string FormatErrorMessage(string name) => IsFrench
        ? "Saisissez une adresse e-mail valide."
        : "أدخل عنوان بريد إلكتروني صالحاً.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalizedPhoneAttribute : ValidationAttribute
{
    private static readonly PhoneAttribute PhoneValidator = new();

    public override bool IsValid(object? value) =>
        value is null || value is string phone && (string.IsNullOrWhiteSpace(phone) || PhoneValidator.IsValid(phone));

    public override string FormatErrorMessage(string name) => IsFrench
        ? "Saisissez un numéro de téléphone valide."
        : "أدخل رقم هاتف صالحاً.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalizedPasswordPolicyAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string password)
            return true;

        return password.Length >= 12
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit);
    }

    public override string FormatErrorMessage(string name) => IsFrench
        ? "Le mot de passe doit contenir au moins 12 caractères, avec une majuscule, une minuscule et un chiffre."
        : "يجب أن تتكون كلمة المرور من 12 حرفاً على الأقل، وأن تشمل حرفاً كبيراً وحرفاً صغيراً ورقماً.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalizedPasswordCompareAttribute : CompareAttribute
{
    public LocalizedPasswordCompareAttribute(string otherProperty) : base(otherProperty) { }

    public override string FormatErrorMessage(string name) => IsFrench
        ? "Les mots de passe ne correspondent pas."
        : "كلمتا المرور غير متطابقتين.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalizedPasskeyNameLengthAttribute : StringLengthAttribute
{
    public LocalizedPasskeyNameLengthAttribute() : base(200) { }

    public override string FormatErrorMessage(string name) => IsFrench
        ? "Le nom de la clé d’accès ne doit pas dépasser 200 caractères."
        : "يجب ألا يتجاوز اسم مفتاح المرور 200 حرف.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}

public sealed class LocalizedAuthenticatorCodeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string code)
            return true;

        var digits = code.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        return digits.Length is 6 or 7 && digits.All(char.IsAsciiDigit);
    }

    public override string FormatErrorMessage(string name) => IsFrench
        ? "Saisissez le code d’authentification à six ou sept chiffres."
        : "أدخل رمز المصادقة المكوّن من ستة أو سبعة أرقام.";

    private static bool IsFrench => CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
}
