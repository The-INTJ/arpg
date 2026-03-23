using System;

namespace ARPG;

[Flags]
public enum MonsterEffectTag
{
    None = 0,
    Defense = 1 << 0,
    Retaliation = 1 << 1,
    Opener = 1 << 2,
    Attrition = 1 << 3,
    PunishesBurst = 1 << 4,
    PunishesSustain = 1 << 5,
    RoomWide = 1 << 6,
    BossSafe = 1 << 7,
}
