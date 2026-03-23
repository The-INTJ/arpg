namespace ARPG;

public partial class WeightedIntOption
{
    public int Value { get; }
    public float Weight { get; }

    public WeightedIntOption(int value, float weight)
    {
        Value = value;
        Weight = weight;
    }
}
