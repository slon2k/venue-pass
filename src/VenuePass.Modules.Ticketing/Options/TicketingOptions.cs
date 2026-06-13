namespace VenuePass.Modules.Ticketing.Options;

public class TicketingOptions
{
    public const string SectionName = "Ticketing";

    public int ReservationExpiryMinutes { get; set; } = 15;

    public int ExpirationSweepIntervalSeconds { get; set; } = 60;

    public TimeSpan ReservationExpiry => TimeSpan.FromMinutes(ReservationExpiryMinutes);

    public TimeSpan ExpirationSweepInterval => TimeSpan.FromSeconds(ExpirationSweepIntervalSeconds);
}