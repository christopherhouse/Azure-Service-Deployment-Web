namespace AzureDeploymentWeb;

public static class StringExtensions
{
    public static string? SanitizeString(this string input)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            return input.Replace("\n", string.Empty)
                         .Replace("\r", string.Empty)
                         .Replace("\t", string.Empty);
        }
        return null;
    }
}