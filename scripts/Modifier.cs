namespace ARPG;

public enum ModifierOp
{
    FlatAdd,       // +N
    PercentAdd,    // +N%
    Multiply,      // ×M
    PercentReduce  // −N%
}

public enum StatTarget
{
    MaxHp,
    AttackDamage,
    MoveSpeed,
    AttackRange
}

public class Modifier
{
    public ModifierOp Op { get; }
    public StatTarget Target { get; }
    public float Value { get; }

    public Modifier(ModifierOp op, StatTarget target, float value)
    {
        Op = op;
        Target = target;
        Value = value;
    }

    public string Description => $"{OpSymbol}{ValueStr} {TargetName}";

    private string OpSymbol => Op switch
    {
        ModifierOp.FlatAdd => "+",
        ModifierOp.PercentAdd => "+",
        ModifierOp.Multiply => "×",
        ModifierOp.PercentReduce => "-",
        _ => ""
    };

    private string ValueStr => Op switch
    {
        ModifierOp.PercentAdd => $"{Value:0}%",
        ModifierOp.PercentReduce => $"{Value:0}%",
        ModifierOp.Multiply => $"{Value:0.#}",
        ModifierOp.FlatAdd when Target == StatTarget.MoveSpeed || Target == StatTarget.AttackRange => $"{Value:0.#}",
        _ => $"{(int)Value}"
    };

    private string TargetName => Target switch
    {
        StatTarget.MaxHp => "HP",
        StatTarget.AttackDamage => "ATK",
        StatTarget.MoveSpeed => "SPD",
        StatTarget.AttackRange => "Range",
        _ => Target.ToString()
    };
}
