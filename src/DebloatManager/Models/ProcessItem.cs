namespace DebloatManager.Models;

public sealed class ProcessItem
{
    public required string Name { get; init; }
    public required List<int> Pids { get; init; }
    public string? FilePath { get; init; }
    public double TotalMemoryMb { get; init; }
    public int InstanceCount => Pids.Count;
}
