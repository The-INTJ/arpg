namespace ARPG;

public partial record DeveloperEffectDescriptor(
    string Id,
    string DisplayName,
    string Description,
    DeveloperEffectKind Kind,
    string DefaultGroupId,
    bool DefaultEnabled = true,
    string OwnerLabel = "",
    int Order = 0);
