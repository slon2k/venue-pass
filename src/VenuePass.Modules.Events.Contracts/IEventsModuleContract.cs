namespace VenuePass.Modules.Events.Contracts;

public interface IEventsModuleContract
{
    Task<ManifestExportDto?> GetManifestForTicketingAsync(Guid manifestId, CancellationToken ct);
}

