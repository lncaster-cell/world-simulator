using WorldSimulator.RoutePathExtractor;

var appBaseDirectory = AppContext.BaseDirectory;
var root = FindRepoRoot(appBaseDirectory);
var baseDir = Path.Combine(root, "data", "regions", "rivia", "routes", "v1");
var settlementsBaseDir = Path.Combine(root, "data", "regions", "rivia", "settlements", "v1");
var maskPath = Path.Combine(baseDir, "world_map_routes_mask.png");
var edgesPath = Path.Combine(baseDir, "route_edges.csv");
var nodesPath = Path.Combine(baseDir, "route_nodes.csv");
var settlementsPath = Path.Combine(settlementsBaseDir, "settlements.json");
var outputPath = Path.Combine(baseDir, "route_paths.json");

Console.WriteLine($"AppContext.BaseDirectory: {appBaseDirectory}");
Console.WriteLine($"Repo root: {root}");
Console.WriteLine($"baseDir: {baseDir}");
Console.WriteLine($"maskPath: {maskPath}");
Console.WriteLine($"edgesPath: {edgesPath}");
Console.WriteLine($"nodesPath: {nodesPath}");
Console.WriteLine($"settlementsPath: {settlementsPath}");
Console.WriteLine($"outputPath: {outputPath}");
Console.WriteLine($"exists mask: {File.Exists(maskPath)}");
Console.WriteLine($"exists edges: {File.Exists(edgesPath)}");
Console.WriteLine($"exists nodes: {File.Exists(nodesPath)}");
Console.WriteLine($"exists settlements: {File.Exists(settlementsPath)}");
Console.WriteLine("Starting route path extraction...");

new RoutePathExtractor().Generate(maskPath, edgesPath, nodesPath, settlementsPath, outputPath);

static string FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WorldSimulator.sln")))
    {
        dir = dir.Parent;
    }

    return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
}
