namespace SmartX.Core.Devices;

/// <summary>
/// One node in a multi-tier deployment tree, e.g. "Facility A" containing
/// "Zone 1" containing "Sub-Zone B" containing a set of leaf sensors.
/// </summary>
public class DeploymentNode
{
    public string Name { get; set; } = string.Empty;
    public List<DeploymentNode> Children { get; set; } = new();
    public List<SensorDevice> Sensors { get; set; } = new();
}

/// <summary>Outcome of validating a deployment tree.</summary>
public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

/// <summary>
/// Technical Requirement: Recursion. Walks a nested device-deployment tree
/// (e.g. validating that a node is safely configured within
/// "Sub-Zone B -> Zone 1 -> Facility A") to confirm every sensor has a MAC
/// address and that no MAC address is duplicated anywhere in the tree,
/// regardless of how many configuration tiers deep it is nested.
/// </summary>
public static class DeploymentValidator
{
    private const int MaxDepth = 32;

    public static ValidationResult Validate(DeploymentNode root)
    {
        var seenMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        ValidateRecursive(root, path: root.Name, seenMacs, errors, depth: 0);

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Recursive case: validate this node's own sensors, then recurse into
    /// every child sub-zone, extending the human-readable path as it goes.
    /// Base case: a node with no children simply returns after validating
    /// its own sensors, unwinding the recursion.
    /// </summary>
    private static void ValidateRecursive(
        DeploymentNode node,
        string path,
        HashSet<string> seenMacs,
        List<string> errors,
        int depth)
    {
        if (depth > MaxDepth)
        {
            errors.Add($"Deployment tree exceeds the maximum nesting depth of {MaxDepth} at '{path}'.");
            return;
        }

        foreach (var sensor in node.Sensors)
        {
            if (string.IsNullOrWhiteSpace(sensor.MacAddress))
            {
                errors.Add($"A sensor with a missing MAC address was found under '{path}'.");
                continue;
            }

            if (!seenMacs.Add(sensor.MacAddress))
                errors.Add($"Duplicate MAC address '{sensor.MacAddress}' found under '{path}'.");
        }

        // Recursive step - one call per child sub-zone.
        foreach (var child in node.Children)
            ValidateRecursive(child, $"{path} -> {child.Name}", seenMacs, errors, depth + 1);
    }

    /// <summary>
    /// Recursively builds the fully-qualified path (e.g. "Facility A -> Zone 1 -> Sub-Zone B")
    /// for the first node found matching <paramref name="targetName"/>, or null if not found.
    /// </summary>
    public static string? FindPath(DeploymentNode node, string targetName, string currentPath = "")
    {
        var path = currentPath.Length == 0 ? node.Name : $"{currentPath} -> {node.Name}";

        if (string.Equals(node.Name, targetName, StringComparison.OrdinalIgnoreCase))
            return path;

        foreach (var child in node.Children)
        {
            var found = FindPath(child, targetName, path);
            if (found is not null)
                return found;
        }

        return null; // base case: exhausted this branch without a match
    }
}
