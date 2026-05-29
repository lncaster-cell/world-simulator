using System.Reflection;
using FluentAssertions;
using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.World;

namespace WorldSimulator.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void SelectSettlement_RestoresEventsForSelectedCityAndRefreshesFoodPreview()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.ToggleRandomEventGenerationCommand.Execute(null);
        viewModel.TriggerRatInfestationEventCommand.Execute(null);
        InvokeOnDayAdvanced(viewModel, day: 1);

        viewModel.DailyFoodEventDelta.Should().Be(-25m);
        GetEventManager(viewModel).ActiveEvents.Should().NotBeEmpty();

        viewModel.SelectSettlementCommand.Execute(RiviaSettlementPresets.HighrockId);

        GetEventManager(viewModel).ActiveEvents.Should().BeEmpty();
        viewModel.ActiveEventEntries.Should().BeEmpty();
        viewModel.DailyFoodEventDelta.Should().Be(0m);
    }

    private static void InvokeOnDayAdvanced(MainWindowViewModel viewModel, int day)
    {
        var method = typeof(MainWindowViewModel).GetMethod("OnDayAdvanced", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(viewModel, [day]);
    }

    private static CityEventManager GetEventManager(MainWindowViewModel viewModel)
    {
        var field = typeof(MainWindowViewModel).GetField("_eventManager", BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        var value = field!.GetValue(viewModel);
        value.Should().BeOfType<CityEventManager>();
        return (CityEventManager)value!;
    }
}
