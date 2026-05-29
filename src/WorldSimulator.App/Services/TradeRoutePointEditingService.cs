using System.Collections.ObjectModel;
using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.Services;

public sealed class TradeRoutePointEditingService
{
    private readonly Dictionary<string, List<MapPointViewModel>> _draftPointsByDestinationId = [];
    private string? _currentDraftDestinationId;

    public void AddPoint(ObservableCollection<MapPointViewModel> points, MapPointViewModel? point)
    {
        if (point is null) return;
        points.Add(CloneClampedPoint(point));
    }

    public void UndoLastPoint(ObservableCollection<MapPointViewModel> points)
    {
        if (points.Count > 0)
        {
            points.RemoveAt(points.Count - 1);
        }
    }

    public void ClearPoints(ObservableCollection<MapPointViewModel> points) => points.Clear();

    public void ClearDrafts()
    {
        _draftPointsByDestinationId.Clear();
        _currentDraftDestinationId = null;
    }

    public void ResetCurrentDraftDestination() => _currentDraftDestinationId = null;

    public void SetCurrentDraftDestination(string destinationId) => _currentDraftDestinationId = destinationId;

    public void SaveCurrentDraft(IEnumerable<MapPointViewModel> points)
    {
        if (string.IsNullOrWhiteSpace(_currentDraftDestinationId)) return;
        _draftPointsByDestinationId[_currentDraftDestinationId] = ClonePoints(points);
    }

    public bool TryLoadCurrentDraft(ObservableCollection<MapPointViewModel> target)
    {
        if (string.IsNullOrWhiteSpace(_currentDraftDestinationId)
            || !_draftPointsByDestinationId.TryGetValue(_currentDraftDestinationId, out var draftPoints))
        {
            return false;
        }

        ReplacePoints(target, draftPoints);
        return true;
    }

    public List<MapPointViewModel> BuildIntermediateRoutePoints(TradeRoute? route)
    {
        if (route is null)
        {
            return [];
        }

        return route.Points
            .Select(point => new MapPointViewModel { X = (double)point.X, Y = (double)point.Y })
            .ToList();
    }

    public List<RoutePoint> BuildFullRoutePoints(
        SimulationWorld world,
        City? origin,
        City? destination,
        IEnumerable<MapPointViewModel> intermediatePoints)
    {
        var fullPoints = new List<RoutePoint>();
        if (origin is not null && TryGetSettlementPoint(world, origin.Id, out var start))
        {
            fullPoints.Add(start);
        }

        fullPoints.AddRange(BuildRoutePoints(intermediatePoints));
        if (destination is not null && TryGetSettlementPoint(world, destination.Id, out var end))
        {
            fullPoints.Add(end);
        }

        return fullPoints;
    }

    public bool TryGetSettlementPoint(SimulationWorld world, string cityId, out RoutePoint point)
    {
        var location = world.FindSettlementMapLocation(cityId);
        if (location is not null)
        {
            point = new RoutePoint { X = location.X, Y = location.Y };
            return true;
        }

        point = new RoutePoint { X = 0m, Y = 0m };
        return false;
    }

    public List<MapPointViewModel> ClonePoints(IEnumerable<MapPointViewModel> points)
    {
        return points.Select(point => new MapPointViewModel { X = point.X, Y = point.Y }).ToList();
    }

    public void ReplacePoints(ObservableCollection<MapPointViewModel> target, IEnumerable<MapPointViewModel> points)
    {
        target.Clear();
        foreach (var point in points)
        {
            target.Add(new MapPointViewModel { X = point.X, Y = point.Y });
        }
    }

    private static IEnumerable<RoutePoint> BuildRoutePoints(IEnumerable<MapPointViewModel> points)
    {
        return points.Select(point => new RoutePoint
        {
            X = (decimal)Math.Clamp(point.X, 0d, 1d),
            Y = (decimal)Math.Clamp(point.Y, 0d, 1d)
        });
    }

    private static MapPointViewModel CloneClampedPoint(MapPointViewModel point)
    {
        return new MapPointViewModel
        {
            X = Math.Clamp(point.X, 0d, 1d),
            Y = Math.Clamp(point.Y, 0d, 1d)
        };
    }
}
