using System;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public partial class DeveloperEffectRegistry
{
    private sealed partial class Entry
    {
        public required string OwnerKey { get; init; }
        public required DeveloperEffectDescriptor Descriptor { get; set; }
        public required bool Enabled { get; set; }
        public required string CurrentGroupId { get; set; }
        public Action<bool> ToggleChanged { get; set; }
        public Action TriggerAction { get; set; }
    }

    private readonly Dictionary<string, Entry> _entries = new();

    public event Action Changed;

    public void Register(
        string ownerKey,
        DeveloperEffectDescriptor descriptor,
        Action<bool> toggleChanged = null,
        Action triggerAction = null)
    {
        string runtimeId = BuildRuntimeId(ownerKey, descriptor.Id);
        if (_entries.TryGetValue(runtimeId, out var existing))
        {
            existing.Descriptor = descriptor;
            existing.ToggleChanged = toggleChanged;
            existing.TriggerAction = triggerAction;
            Changed?.Invoke();
            return;
        }

        var entry = new Entry
        {
            OwnerKey = ownerKey,
            Descriptor = descriptor,
            Enabled = descriptor.DefaultEnabled,
            CurrentGroupId = descriptor.DefaultGroupId,
            ToggleChanged = toggleChanged,
            TriggerAction = triggerAction,
        };
        _entries[runtimeId] = entry;
        toggleChanged?.Invoke(entry.Enabled);
        Changed?.Invoke();
    }

    public void RemoveOwner(string ownerKey)
    {
        var runtimeIds = _entries
            .Where(pair => pair.Value.OwnerKey == ownerKey)
            .Select(pair => pair.Key)
            .ToArray();

        if (runtimeIds.Length == 0)
            return;

        foreach (string runtimeId in runtimeIds)
            _entries.Remove(runtimeId);

        Changed?.Invoke();
    }

    public bool IsEnabled(string ownerKey, string effectId)
    {
        return TryGetEntry(BuildRuntimeId(ownerKey, effectId), out var entry)
            ? entry.Enabled
            : true;
    }

    public void SetEnabled(string runtimeId, bool enabled)
    {
        if (!TryGetEntry(runtimeId, out var entry) || entry.Enabled == enabled)
            return;

        entry.Enabled = enabled;
        entry.ToggleChanged?.Invoke(enabled);
        Changed?.Invoke();
    }

    public void SetGroupEnabled(string groupId, bool enabled)
    {
        bool changed = false;
        foreach (var pair in _entries)
        {
            if (pair.Value.CurrentGroupId != groupId || pair.Value.Enabled == enabled)
                continue;

            pair.Value.Enabled = enabled;
            pair.Value.ToggleChanged?.Invoke(enabled);
            changed = true;
        }

        if (changed)
            Changed?.Invoke();
    }

    public void SetCurrentGroup(string runtimeId, string groupId)
    {
        if (!TryGetEntry(runtimeId, out var entry) || entry.CurrentGroupId == groupId)
            return;

        entry.CurrentGroupId = groupId;
        Changed?.Invoke();
    }

    public bool TryTrigger(string runtimeId)
    {
        if (!TryGetEntry(runtimeId, out var entry) || entry.TriggerAction == null)
            return false;

        entry.TriggerAction();
        Changed?.Invoke();
        return true;
    }

    public IReadOnlyList<DeveloperEffectGroupSnapshot> SnapshotGroups()
    {
        return _entries
            .Select(pair => new DeveloperEffectSnapshot(
                pair.Key,
                pair.Value.Descriptor.Id,
                pair.Value.OwnerKey,
                string.IsNullOrWhiteSpace(pair.Value.Descriptor.OwnerLabel) ? pair.Value.OwnerKey : pair.Value.Descriptor.OwnerLabel,
                pair.Value.Descriptor.DisplayName,
                pair.Value.Descriptor.Description,
                pair.Value.Descriptor.Kind,
                pair.Value.CurrentGroupId,
                pair.Value.Enabled,
                pair.Value.TriggerAction != null,
                pair.Value.Descriptor.Order))
            .GroupBy(snapshot => snapshot.GroupId)
            .OrderBy(group => DeveloperEffectGroups.SortOrder(group.Key))
            .ThenBy(group => DeveloperEffectGroups.DisplayName(group.Key), StringComparer.Ordinal)
            .Select(group =>
            {
                var effects = group
                    .OrderBy(effect => effect.Order)
                    .ThenBy(effect => effect.DisplayName, StringComparer.Ordinal)
                    .ToArray();
                int enabledCount = effects.Count(effect => effect.Enabled);
                return new DeveloperEffectGroupSnapshot(
                    group.Key,
                    DeveloperEffectGroups.DisplayName(group.Key),
                    enabledCount,
                    effects.Length,
                    effects.Length > 0 && enabledCount == effects.Length,
                    effects);
            })
            .ToArray();
    }

    private bool TryGetEntry(string runtimeId, out Entry entry)
    {
        return _entries.TryGetValue(runtimeId, out entry);
    }

    public static string BuildRuntimeId(string ownerKey, string effectId)
    {
        return $"{ownerKey}::{effectId}";
    }
}
