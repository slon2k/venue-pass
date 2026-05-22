using VenuePass.BuildingBlocks.Application;

namespace VenuePass.Modules.Events.Features.CreateVenue;

public static class CreateVenueErrors
{
    public static Error VenueAlreadyExists(string name, string city) => Error.Conflict(
        code: "Events.CreateVenue.VenueAlreadyExists",
        message: $"A venue with the name '{name}' already exists in the city '{city}'.");

    public static ValidationError InvalidData(
        IReadOnlyList<ValidationErrorDetail> details) =>
        ValidationError.Create(
            code: "Events.CreateVenue.InvalidData",
            message: "Invalid venue data.",
            details: details);

    public static ValidationError InvalidData(string message) =>
        ValidationError.Create(
            code: "Events.CreateVenue.InvalidData",
            message: "Invalid venue data.",
            details: [new ValidationErrorDetail(string.Empty, message)]);
}