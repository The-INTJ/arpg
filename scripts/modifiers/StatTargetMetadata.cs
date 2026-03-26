namespace ARPG;

public readonly record struct StatTargetMetadata(
    StatTarget Target,
    string DisplayName,
    bool IsDiscrete,
    float MinimumValue,
    int DisplayOrder,
    bool AllowFlexibleFlatAdd = true,
    bool AllowFlexiblePercentAdd = true)
{
    public bool AllowsFlexibleModifier(ModifierOp op) => op switch
    {
        ModifierOp.FlatAdd => AllowFlexibleFlatAdd,
        ModifierOp.PercentAdd => AllowFlexiblePercentAdd,
        ModifierOp.Multiply => AllowFlexiblePercentAdd,
        ModifierOp.PercentReduce => AllowFlexiblePercentAdd,
        _ => false
    };
}
