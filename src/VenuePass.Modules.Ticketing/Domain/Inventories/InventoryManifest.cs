namespace VenuePass.Modules.Ticketing.Domain.Inventories;

public sealed class InventoryManifest
{
    public InventoryManifest(
        IReadOnlyList<InventorySectionInput> sections,
        IReadOnlyList<InventoryGeneralAdmissionAreaInput> generalAdmissionAreas)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(generalAdmissionAreas);

        Sections = sections;
        GeneralAdmissionAreas = generalAdmissionAreas;
    }

    public IReadOnlyList<InventorySectionInput> Sections { get; }

    public IReadOnlyList<InventoryGeneralAdmissionAreaInput> GeneralAdmissionAreas { get; }
}

public sealed class InventorySectionInput
{
    public InventorySectionInput(string name, IReadOnlyList<InventoryRowInput> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rows);

        Name = name;
        Rows = rows;
    }

    public string Name { get; }

    public IReadOnlyList<InventoryRowInput> Rows { get; }
}

public sealed class InventoryRowInput
{
    public InventoryRowInput(string label, IReadOnlyList<InventorySeatInput> seats)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(seats);

        Label = label;
        Seats = seats;
    }

    public string Label { get; }

    public IReadOnlyList<InventorySeatInput> Seats { get; }
}

public sealed class InventorySeatInput
{
    public InventorySeatInput(Guid seatId, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        SeatId = seatId;
        Label = label;
    }

    public Guid SeatId { get; }

    public string Label { get; }
}

public sealed class InventoryGeneralAdmissionAreaInput
{
    public InventoryGeneralAdmissionAreaInput(Guid areaId, string name, int capacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        AreaId = areaId;
        Name = name;
        Capacity = capacity;
    }

    public Guid AreaId { get; }

    public string Name { get; }

    public int Capacity { get; }
}