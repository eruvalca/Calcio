namespace Calcio.Shared.Extensions.Shared;

public static class StringExtensions
{
    extension(string? source)
    {
        /// <summary>
        /// Determines whether this string contains the specified value using case-insensitive comparison.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <returns>true if the value parameter occurs within this string; otherwise, false. Returns false if either string is null.</returns>
        public bool ContainsIgnoreCase(string? value)
            => source is not null && value is not null && source.Contains(value, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether this string equals the specified value using case-insensitive comparison.
        /// </summary>
        /// <param name="value">The string to compare to this instance.</param>
        /// <returns>true if the value parameter equals this string; otherwise, false. Returns true if both strings are null.</returns>
        public bool EqualsIgnoreCase(string? value)
            => string.Equals(source, value, StringComparison.OrdinalIgnoreCase);
    }
}
