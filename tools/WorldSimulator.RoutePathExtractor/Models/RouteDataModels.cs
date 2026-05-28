namespace WorldSimulator.RoutePathExtractor;

internal sealed record Node(string NodeId, string Name, bool IsStub);
internal sealed record Edge(string RouteId, string FromNode, string ToNode, string RouteType, bool IsStub);
internal readonly record struct AnchorSearchResult((int X, int Y) Point, double Distance);
internal readonly record struct Bounds(int MinX, int MaxX, int MinY, int MaxY);
