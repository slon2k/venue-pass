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

    public static void ThrowIfNotInRange(this decimal argument, string argumentName, decimal minValue, decimal maxValue)
    {
        if (argument < minValue || argument > maxValue)
        {
            throw new ArgumentOutOfRangeException(argumentName, $"'{argumentName}' must be between {minValue} and {maxValue}.");
        }
    }

    public static void ThrowIfLengthNotEqual(this string argument, string argumentName, int requiredLength)
    {
        if (argument.Length != requiredLength)
        {
            throw new ArgumentException($"'{argumentName}' must be exactly {requiredLength} characters long.", argumentName);
        }
    }

    public static void ThrowIfEmpty(this Guid argument, string argumentName)
    {
        if (argument == Guid.Empty)
        {
            throw new ArgumentException($"'{argumentName}' must be a non-empty GUID.", argumentName);
        }
    }
}