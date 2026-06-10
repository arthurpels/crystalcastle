public interface IPowerSource
{
    bool      IsActive  { get; }
    PowerNode OutputNode { get; }
}
