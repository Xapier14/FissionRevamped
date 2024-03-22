namespace FissionRevamped.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : string.Concat(value.AsSpan(0, maxChars), "...");
        }
    }
}
