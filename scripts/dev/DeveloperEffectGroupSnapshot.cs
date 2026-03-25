using System.Collections.Generic;

namespace ARPG;

public partial record DeveloperEffectGroupSnapshot(
    string GroupId,
    string DisplayName,
    int EnabledCount,
    int TotalCount,
    bool AllEnabled,
    IReadOnlyList<DeveloperEffectSnapshot> Effects);
