namespace LicoresMaduro.API.Helpers;

public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to at most <paramref name="maxLength"/> characters.
    /// Returns null when the input is null.
    /// </summary>
    public static string Trunc(this string s, int maxLength) =>
        s.Length <= maxLength ? s : s[..maxLength];
}
