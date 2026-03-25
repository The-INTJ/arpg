namespace ARPG;

public partial record DeveloperEffectSnapshot(
    string RuntimeId,
    string LocalId,
    string OwnerKey,
    string OwnerLabel,
    string DisplayName,
    string Description,
    DeveloperEffectKind Kind,
    string GroupId,
    bool Enabled,
    bool CanTrigger,
    int Order);
