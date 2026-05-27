namespace VenuePass.BuildingBlocks.Domain;

public readonly record struct UserId(Guid Value)
{
    public static UserId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(UserId id) => id.Value;
    public override string ToString() => Value.ToString();
}
