namespace ARPG;

/// <summary>
/// Tracks dark energy accumulated per chunk. Each enemy killed grants energy;
/// once the threshold is met the bridge can be activated. Excess carries over.
/// </summary>
public class DarkEnergy
{
	public int Current { get; private set; }
	public int Threshold { get; }

	public bool IsThresholdMet => Current >= Threshold;
	public float FillPercent => Threshold > 0 ? (float)Current / Threshold : 1f;
	public int Excess => Current > Threshold ? Current - Threshold : 0;

	/// <summary>
	/// Energy available for building structures. If the bridge is already built,
	/// all current energy is spendable; otherwise only excess above the threshold.
	/// </summary>
	public int Spendable(bool bridgeBuilt) => bridgeBuilt ? Current : Excess;

	public DarkEnergy(int threshold, int initialEnergy = 0)
	{
		Threshold = threshold;
		Current = initialEnergy;
	}

	public void Add(int amount)
	{
		Current += amount;
	}

	/// <summary>
	/// Attempt to spend energy on a player-built structure. Returns false if insufficient.
	/// </summary>
	public bool TrySpend(int amount, bool bridgeBuilt)
	{
		if (amount <= 0 || amount > Spendable(bridgeBuilt))
			return false;

		Current -= amount;
		return true;
	}

	public static int EnergyForKill(bool isBoss, bool isElite)
	{
		if (isBoss) return 3;
		if (isElite) return 2;
		return 1;
	}

	/// <summary>Dark energy threshold for a given room number.</summary>
	public static int ThresholdForRoom(int room)
	{
		return room switch
		{
			1 => 3,
			2 => 5,
			3 => 6,
			_ => 3,
		};
	}
}
