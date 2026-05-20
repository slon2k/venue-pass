namespace VenuePass.BuildingBlocks.Infrastructure;

public interface ICurrentUser
{
    string Id { get; }

    bool IsAuthenticated { get; }

    string? Name { get; }
}
