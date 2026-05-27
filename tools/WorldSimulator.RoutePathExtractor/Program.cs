using WorldSimulator.RoutePathExtractor;

var root = FindRepoRoot(AppContext.BaseDirectory);
var baseDir = Path.Combine(root, "data", "regions", "rivia", "routes", "v1");

new RoutePathExtractor().Generate(
    Path.Combine(baseDir, "world_map_routes_mask.png"),
    Path.Combine(baseDir, "route_edges.csv"),
    Path.Combine(baseDir, "route_nodes.csv"),
    Path.Combine(baseDir, "route_paths.json"));

Console.WriteLine($"Generated route_paths.json in {baseDir}");

static string FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WorldSimulator.sln")))
    {
        dir = dir.Parent;
    }

    return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
}
