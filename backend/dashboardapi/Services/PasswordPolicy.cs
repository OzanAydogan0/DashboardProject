namespace dashboardapi.Services;

public static class PasswordPolicy
{
    public const string RequirementsMessage =
        "Şifre en az 12 karakter; büyük harf, küçük harf, rakam ve özel karakter içermelidir.";

    public static bool IsStrong(string? password) =>
        !string.IsNullOrWhiteSpace(password) &&
        password.Length >= 12 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(character => !char.IsLetterOrDigit(character));
}
