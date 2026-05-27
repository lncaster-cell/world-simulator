using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var root = FindRepoRoot(AppContext.BaseDirectory);
var baseDir = Path.Combine(root, "data", "regions", "rivia", "routes", "v1");
var maskPath = Path.Combine(baseDir, "world_map_routes_mask.png");
var edgesPath = Path.Combine(baseDir, "route_edges.csv");
var nodesPath = Path.Combine(baseDir, "route_nodes.csv");
var outputPath = Path.Combine(baseDir, "route_paths.json");

var nodes = LoadNodes(nodesPath);
var edges = LoadEdges(edgesPath);
var settlements = BuildSettlementCoordinates();
using var image = Image.Load<Rgba32>(maskPath);
var landMask = BuildMask(image, IsRoad);
var seaMask = BuildMask(image, IsSea);

var paths = new List<object>();
foreach (var edge in edges.Where(e => !e.IsStub))
{
    if (!nodes.TryGetValue(edge.FromNode, out var fromNode) || !nodes.TryGetValue(edge.ToNode, out var toNode)) continue;
    if (!settlements.TryGetValue(NormalizeName(fromNode.Name), out var from) || !settlements.TryGetValue(NormalizeName(toNode.Name), out var to)) continue;
    var mask = edge.RouteType.Equals("sea", StringComparison.OrdinalIgnoreCase) ? seaMask : landMask;
    var start = FindNearestMaskPixel(mask, image.Width, image.Height, from);
    var end = FindNearestMaskPixel(mask, image.Width, image.Height, to);
    if (start is null || end is null) throw new InvalidOperationException($"Mask anchor not found for route {edge.RouteId}.");
    var pixelPath = FindPath(mask, image.Width, image.Height, start.Value, end.Value);
    if (pixelPath.Count < 2) throw new InvalidOperationException($"Path not found in mask for route {edge.RouteId}.");
    var simplified = Simplify(pixelPath, 2.0);
    paths.Add(new
    {
        source_route_id = edge.RouteId,
        trade_route_id = BuildTradeRouteId(edge.FromNode, edge.ToNode, edge.RouteType),
        route_type = edge.RouteType.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "road",
        points = simplified.Select(p => new { x = Math.Clamp((double)p.X / (image.Width - 1), 0d, 1d), y = Math.Clamp((double)p.Y / (image.Height - 1), 0d, 1d) })
    });
}

var payload = new { schema_version = "rivia_route_paths_v1", region_id = "RIVIA", paths };
var tempOutputPath = outputPath + ".tmp";
await File.WriteAllTextAsync(tempOutputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
File.Move(tempOutputPath, outputPath, overwrite: true);
Console.WriteLine($"Generated {paths.Count} paths -> {outputPath}");

static string FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WorldSimulator.sln"))) dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
}

static bool[] BuildMask(Image<Rgba32> image, Func<Rgba32, bool> pred)
{
    var mask = new bool[image.Width * image.Height];
    for (var y = 0; y < image.Height; y++)
    for (var x = 0; x < image.Width; x++)
        mask[(y * image.Width) + x] = pred(image[x, y]);
    return mask;
}
static bool IsRoad(Rgba32 p) => p.A >= 128 && p.R >= 180 && p.B >= 180 && p.G <= 120;
static bool IsSea(Rgba32 p) => p.A >= 128 && p.G >= 180 && p.B >= 180 && p.R <= 120;

static (int X,int Y)? FindNearestMaskPixel(bool[] mask,int width,int height,(double X,double Y) pt)
{
    var sx=(int)Math.Round(pt.X*(width-1)); var sy=(int)Math.Round(pt.Y*(height-1));
    var bestDist=double.MaxValue; (int,int)? best=null;
    for(var y=Math.Max(0,sy-40); y<=Math.Min(height-1,sy+40); y++)
    for(var x=Math.Max(0,sx-40); x<=Math.Min(width-1,sx+40); x++)
    {
        if(!mask[(y*width)+x]) continue; var d=(x-sx)*(x-sx)+(y-sy)*(y-sy); if(d<bestDist){bestDist=d; best=(x,y);}    }
    return best;
}
static List<(int X,int Y)> FindPath(bool[] mask,int w,int h,(int X,int Y) s,(int X,int Y) t)
{
    var prev=new Dictionary<int,int>(); var q=new Queue<int>(); var seen=new HashSet<int>();
    int Sid=s.Y*w+s.X, Tid=t.Y*w+t.X; q.Enqueue(Sid); seen.Add(Sid);
    int[] d={-1,0,1};
    while(q.Count>0){var cur=q.Dequeue(); if(cur==Tid) break; var cx=cur%w; var cy=cur/w;
        foreach(var dx in d) foreach(var dy in d){ if(dx==0&&dy==0) continue; var nx=cx+dx; var ny=cy+dy; if(nx<0||ny<0||nx>=w||ny>=h) continue; var ni=ny*w+nx; if(!mask[ni]||!seen.Add(ni)) continue; prev[ni]=cur; q.Enqueue(ni);} }
    if(!seen.Contains(Tid)) return [];
    var path=new List<(int,int)>(); for(var at=Tid;;){path.Add((at%w,at/w)); if(at==Sid) break; at=prev[at];} path.Reverse(); return path;
}
static List<(int X,int Y)> Simplify(List<(int X,int Y)> pts,double eps){if(pts.Count<3) return pts; var keep=new bool[pts.Count]; keep[0]=keep[^1]=true; SimplifyRec(pts,0,pts.Count-1,eps,keep); return pts.Where((_,i)=>keep[i]).ToList();}
static void SimplifyRec(List<(int X,int Y)> pts,int s,int e,double eps,bool[] keep){double max=0;int idx=-1;for(int i=s+1;i<e;i++){var d=Perp(pts[i],pts[s],pts[e]); if(d>max){max=d;idx=i;}} if(max>eps&&idx>0){keep[idx]=true; SimplifyRec(pts,s,idx,eps,keep); SimplifyRec(pts,idx,e,eps,keep);} }
static double Perp((int X,int Y) p,(int X,int Y) a,(int X,int Y) b){double dx=b.X-a.X,dy=b.Y-a.Y; if(dx==0&&dy==0) return Math.Sqrt(Math.Pow(p.X-a.X,2)+Math.Pow(p.Y-a.Y,2)); return Math.Abs(dy*p.X-dx*p.Y+b.X*a.Y-b.Y*a.X)/Math.Sqrt(dx*dx+dy*dy);} 

record Node(string NodeId,string Name,bool IsStub);
record Edge(string RouteId,string FromNode,string ToNode,string RouteType,bool IsStub);
static Dictionary<string,Node> LoadNodes(string path)=>ReadCsv(path).Skip(1).Select(c=>new Node(c[0],c[1],bool.Parse(c[8]))).ToDictionary(x=>x.NodeId,StringComparer.OrdinalIgnoreCase);
static List<Edge> LoadEdges(string path)=>ReadCsv(path).Skip(1).Select(c=>new Edge(c[0],c[1],c[2],c[3],bool.Parse(c[8]))).ToList();
static IEnumerable<string[]> ReadCsv(string path){foreach(var line in File.ReadLines(path)){yield return SplitCsv(line).ToArray();}}
static List<string> SplitCsv(string line){var r=new List<string>(); var cur=""; bool q=false; foreach(var ch in line){if(ch=='"'){q=!q; continue;} if(ch==','&&!q){r.Add(cur); cur="";} else cur+=ch;} r.Add(cur); return r;}
static string NormalizeName(string name)=>name.ToLowerInvariant().Replace('ö','o').Replace('-', '_').Replace(' ', '_') switch {"tokrus"=>"thokur_rus", var n=>n};
static readonly Dictionary<string,string> NodeMap = new(StringComparer.OrdinalIgnoreCase){["N_HIGHROCK"]="highrock",["N_MLYNEK"]="mlynek",["N_WARDMARK"]="wardmark",["N_RIVENSTAL"]="rivenstal",["N_GAVERN"]="gavern",["N_BRNO"]="brno",["N_WODENZ"]="wodenz",["N_GOTHA"]="gotha",["N_TOKRUS"]="thokur_rus"};
static string BuildTradeRouteId(string from,string to,string type){ var suffix = type.Equals("sea", StringComparison.OrdinalIgnoreCase) ? "sea" : "land"; return $"{MapNodeOrName(from)}_{MapNodeOrName(to)}_{suffix}"; }
static string MapNodeOrName(string value)=>NodeMap.GetValueOrDefault(value, NormalizeName(value));
static Dictionary<string,(double X,double Y)> BuildSettlementCoordinates()=>new(StringComparer.OrdinalIgnoreCase)
{
    ["gotha"]=(0.6664,0.2322),["rivenstal"]=(0.4824,0.45),["gavern"]=(0.5066,0.5963),["mlynek"]=(0.2833,0.2487),["brno"]=(0.4527,0.7448),["wodenz"]=(0.8036,0.9604),["wardmark"]=(0.0380,0.4027),["highrock"]=(0.1579,0.2179),["thokur_rus"]=(0.8652,0.4753)
};
