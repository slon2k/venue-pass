namespace VenuePass.BuildingBlocks.Extensions;

public static class ArgumentExtensions
{
    public static void ThrowIfTooLong(this string argument, string argumentName, int maxLength)
    {
        if (argument.Length > maxLength)
        {
            throw new ArgumentException($"'{argumentName}' cannot be longer than {maxLength} characters.", argumentName);
        }
    }

    public static void ThrowIfTooShort(this string argument, string argumentName, int minLength)
    {
        if (argument.Length < minLength)
        {
            throw new ArgumentException($"'{argumentName}' cannot be shorter than {minLength} characters.", argumentName);
        }
    }

    public static void ThrowIfNotInRange(this int argument, string argumentName, int minValue, int maxValue)
    {
        if (argument < minValue || argument > maxValue)
        {
            throw new ArgumentOutOfRangeException(argumentName, $"'{argumentName}' must be between {minValue} and {maxValue}.");
        }
    }
}