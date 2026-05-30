using System.Collections.ObjectModel;
using WorldSimulator.App.Services;
using WorldSimulator.Core.Cities;
using WorldSimulator.Core.Trade;

namespace WorldSimulator.App.ViewModels;

public sealed class TradeRouteAuthoringSession
{
    public const string EmptyRouteIdDisplay = "RouteId: —";
    public const decimal DefaultDistanceDays = 1m;
    public const string DefaultDistanceDaysInput = "1.0";

    public TradeRoute? SelectedTradeRouteForAuthoring { get; private set; }
    public City? RouteAuthoringOriginSettlement { get; private set; }
    public City? RouteAuthoringDestinationCandidateSettlement { get; private set; }
    public City? ActiveRouteAuthoringDestinationSettlement { get; private set; }
    public string RouteAuthoringRouteIdDisplay { get; private set; } = EmptyRouteIdDisplay;
    public decimal SelectedTradeRouteDistanceDays { get; private set; } = DefaultDistanceDays;
    public string SelectedTradeRouteDistanceDaysInput { get; private set; } = DefaultDistanceDaysInput;
    public ObservableCollection<City> RouteAuthoringDestinationSettlements { get; } = [];

    public int RouteAuthoringDestinationCount => RouteAuthoringDestinationSettlements.Count;
    public bool CanAddMoreRouteAuthoringDestinations => RouteAuthoringDestinationSettlements.Count < TradeRouteSelectionService.MaxDestinationCount;
    public bool HasSelectedTradeRouteForAuthoring => SelectedTradeRouteForAuthoring is not null;

    public bool SetRouteAuthoringOriginSettlement(City? settlement)
    {
        if (RouteAuthoringOriginSettlement == settlement) return false;
        RouteAuthoringOriginSettlement = settlement;
        return true;
    }

    public bool SetRouteAuthoringDestinationCandidateSettlement(City? settlement)
    {
        if (RouteAuthoringDestinationCandidateSettlement == settlement) return false;
        RouteAuthoringDestinationCandidateSettlement = settlement;
        return true;
    }

    public bool SetActiveRouteAuthoringDestinationSettlement(City? settlement)
    {
        if (ActiveRouteAuthoringDestinationSettlement == settlement) return false;
        ActiveRouteAuthoringDestinationSettlement = settlement;
        return true;
    }

    public bool SetSelectedTradeRouteForAuthoring(TradeRoute? tradeRoute)
    {
        if (SelectedTradeRouteForAuthoring == tradeRoute) return false;
        SelectedTradeRouteForAuthoring = tradeRoute;
        return true;
    }

    public bool SetSelectedTradeRouteDistanceDays(decimal distanceDays)
    {
        if (SelectedTradeRouteDistanceDays == distanceDays) return false;
        SelectedTradeRouteDistanceDays = distanceDays;
        return true;
    }

    public bool SetSelectedTradeRouteDistanceDaysInput(string distanceDaysInput)
    {
        if (SelectedTradeRouteDistanceDaysInput == distanceDaysInput) return false;
        SelectedTradeRouteDistanceDaysInput = distanceDaysInput;
        return true;
    }

    public bool AddRouteAuthoringDestination(City destination)
    {
        RouteAuthoringDestinationSettlements.Add(destination);
        if (ActiveRouteAuthoringDestinationSettlement is not null)
        {
            return false;
        }

        ActiveRouteAuthoringDestinationSettlement = destination;
        return true;
    }

    public void ResetRouteAuthoringOriginState(TradeRoutePointEditingService pointEditingService, Action clearEditedRoutePoints)
    {
        pointEditingService.ClearDrafts();
        RouteAuthoringDestinationSettlements.Clear();
        ActiveRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationCandidateSettlement = null;
        SelectedTradeRouteForAuthoring = null;
        ResetDistanceDays();
        RouteAuthoringRouteIdDisplay = EmptyRouteIdDisplay;
        clearEditedRoutePoints();
    }

    public void ResetRouteAuthoringOriginState()
    {
        RouteAuthoringDestinationSettlements.Clear();
        ActiveRouteAuthoringDestinationSettlement = null;
        RouteAuthoringDestinationCandidateSettlement = null;
        SelectedTradeRouteForAuthoring = null;
        ResetDistanceDays();
        RouteAuthoringRouteIdDisplay = EmptyRouteIdDisplay;
    }

    public void ResetForWorldReset()
    {
        RouteAuthoringOriginSettlement = null;
        ResetRouteAuthoringOriginState();
    }

    public bool LoadActiveRouteDraftOrExisting(
        TradeRoutePointEditingService pointEditingService,
        ObservableCollection<MapPointViewModel> editedRoutePoints,
        Action clearEditedRoutePoints)
    {
        clearEditedRoutePoints();
        if (RouteAuthoringOriginSettlement is null || ActiveRouteAuthoringDestinationSettlement is null)
        {
            pointEditingService.ResetCurrentDraftDestination();
            RouteAuthoringRouteIdDisplay = EmptyRouteIdDisplay;
            return false;
        }

        pointEditingService.SetCurrentDraftDestination(ActiveRouteAuthoringDestinationSettlement.Id);
        pointEditingService.TryLoadCurrentDraft(editedRoutePoints);
        return true;
    }

    public bool ApplyRouteAuthoringSelection(TradeRouteSelectionResult selection)
    {
        SelectedTradeRouteForAuthoring = selection.SelectedRoute;
        SelectedTradeRouteDistanceDays = selection.DistanceDays;
        SelectedTradeRouteDistanceDaysInput = selection.DistanceDaysInput;
        RouteAuthoringRouteIdDisplay = selection.RouteIdDisplay;
        return selection.SelectedRoute is null && selection.ShouldClearEditedPoints;
    }

    public void ApplySelectedRouteDistance(TradeRoute selectedRoute, string distanceDaysInput)
    {
        SelectedTradeRouteDistanceDays = selectedRoute.DistanceDays;
        SelectedTradeRouteDistanceDaysInput = distanceDaysInput;
    }

    private void ResetDistanceDays()
    {
        SelectedTradeRouteDistanceDays = DefaultDistanceDays;
        SelectedTradeRouteDistanceDaysInput = DefaultDistanceDaysInput;
    }
}
