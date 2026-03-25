using System.Linq;
using Xunit;

namespace ARPG.Tests;

public partial class DeveloperEffectRegistryTests
{
    [Fact]
    public void RegisterUsesDefaultGroupAndEnabledState()
    {
        var registry = new DeveloperEffectRegistry();
        registry.Register(
            "player-1",
            new DeveloperEffectDescriptor(
                "zone_bounds_guard",
                "Zone Bounds Guard",
                "Track zone boundary recovery.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Boundary,
                DefaultEnabled: true,
                OwnerLabel: "Player"));

        var group = Assert.Single(registry.SnapshotGroups());
        Assert.Equal(DeveloperEffectGroups.Boundary, group.GroupId);
        Assert.True(group.AllEnabled);
        Assert.Equal("Zone Bounds Guard", Assert.Single(group.Effects).DisplayName);
    }

    [Fact]
    public void CustomGroupAssignmentSurvivesEnableDisableChanges()
    {
        var registry = new DeveloperEffectRegistry();
        registry.Register(
            "world-1",
            new DeveloperEffectDescriptor(
                "bridge_energy_requirement",
                "Bridge Energy Requirement",
                "Require threshold energy before bridge readiness.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Progression,
                DefaultEnabled: true,
                OwnerLabel: "World"));

        string runtimeId = DeveloperEffectRegistry.BuildRuntimeId("world-1", "bridge_energy_requirement");
        registry.SetCurrentGroup(runtimeId, DeveloperEffectGroups.Boundary);
        registry.SetEnabled(runtimeId, false);

        var group = Assert.Single(registry.SnapshotGroups());
        Assert.Equal(DeveloperEffectGroups.Boundary, group.GroupId);
        Assert.False(Assert.Single(group.Effects).Enabled);
    }

    [Fact]
    public void RemoveOwnerCleansItsEntries()
    {
        var registry = new DeveloperEffectRegistry();
        registry.Register(
            "aggro-1",
            new DeveloperEffectDescriptor(
                "encounter_watch",
                "Encounter Watch",
                "Watch for nearby enemies.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Encounter,
                OwnerLabel: "Aggro"));
        registry.Register(
            "aggro-1",
            new DeveloperEffectDescriptor(
                "encounter_commit",
                "Encounter Commit",
                "Start combat after the watch timer.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Encounter,
                OwnerLabel: "Aggro"));

        Assert.Equal(2, registry.SnapshotGroups().SelectMany(group => group.Effects).Count());

        registry.RemoveOwner("aggro-1");

        Assert.Empty(registry.SnapshotGroups());
    }
}
