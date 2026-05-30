using System.Reflection;
using FluentAssertions;
using WorldSimulator.App.ViewModels;
using WorldSimulator.Core.Events;
using WorldSimulator.Core.Simulation;
using WorldSimulator.Core.World;
using Xunit;

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

    [Fact]
    public void OnDayAdvanced_SynchronizesSelectedCityEventsFromSimulationStateWithoutCitySwitch()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.ToggleRandomEventGenerationCommand.Execute(null);
        viewModel.TriggerDiseaseEventCommand.Execute(null);
        viewModel.TriggerFireEventCommand.Execute(null);

        var eventManager = GetEventManager(viewModel);
        var selectedCityId = GetWorld(viewModel).SelectedCityId;
        var simulationService = GetWorldSimulationService(viewModel);
        var eventState = simulationService.ExportEventState();
        var selectedManager = eventState.GetOrCreateManager(selectedCityId);
        selectedManager.Restore(eventManager.ActiveEvents, eventManager.CompletedEvents);
        selectedManager.AddEvent(CityEventPresets.CreatePortStorm(currentDay: 1));
        simulationService.ImportEventState(eventState, selectedCityId);

        InvokeOnDayAdvanced(viewModel, day: 1);

        eventManager.ActiveEvents.Should().ContainSingle(e =>
            e.Id == "disease" && e.RemainingDays == 2,
            "ручное событие должно уменьшать RemainingDays после продвижения дня");
        eventManager.CompletedEvents.Should().ContainSingle(e =>
            e.Id == "fire" && e.RemainingDays == 0,
            "завершённое событие должно попасть в CompletedEvents");
        eventManager.ActiveEvents.Should().ContainSingle(e =>
            e.Id == "port_storm" && e.RemainingDays == 1,
            "симулированно добавленное событие должно быть восстановлено из состояния симуляции");

        viewModel.ActiveEventEntries.Should().Contain(entry =>
            entry.Contains("Болезнь", StringComparison.Ordinal) &&
            entry.Contains("осталось 2 дн.", StringComparison.Ordinal));
        viewModel.ActiveEventEntries.Should().Contain(entry =>
            entry.Contains("Шторм у порта", StringComparison.Ordinal) &&
            entry.Contains("осталось 1 дн.", StringComparison.Ordinal));
        viewModel.CompletedEventEntries.Should().Contain(entry =>
            entry.Contains("Пожар", StringComparison.Ordinal));
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

    private static SimulationWorld GetWorld(MainWindowViewModel viewModel)
    {
        var field = typeof(MainWindowViewModel).GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        var value = field!.GetValue(viewModel);
        value.Should().BeOfType<SimulationWorld>();
        return (SimulationWorld)value!;
    }

    private static WorldSimulationService GetWorldSimulationService(MainWindowViewModel viewModel)
    {
        var field = typeof(MainWindowViewModel).GetField("_worldSimulationService", BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        var value = field!.GetValue(viewModel);
        value.Should().BeOfType<WorldSimulationService>();
        return (WorldSimulationService)value!;
    }
}
