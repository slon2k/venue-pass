namespace VenuePass.BuildingBlocks.Application;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    Conflict = 2,
    Forbidden = 3,
    Unauthorized = 4,
    NotFound = 5,
    Concurrency = 6,
    RateLimit = 7,
    Unavailable = 8,
    Unexpected = 9
}
