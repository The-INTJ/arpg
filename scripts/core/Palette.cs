using Godot;

namespace ARPG;

public static class Palette
{
	// UI
	public static readonly Color BgDark = new(0.12f, 0.08f, 0.05f);
	public static readonly Color TextLight = new(0.96f, 0.90f, 0.80f);
	public static readonly Color Accent = new(0.83f, 0.65f, 0.22f);
	public static readonly Color ButtonBg = new(0.55f, 0.35f, 0.15f);
	public static readonly Color ButtonHover = new(0.70f, 0.45f, 0.18f);
	public static readonly Color ButtonDisabled = new(0.3f, 0.25f, 0.2f);
	public static readonly Color TextDisabled = new(0.5f, 0.45f, 0.4f);

	// World
	public static readonly Color Floor = new(0.65f, 0.55f, 0.35f);
	public static readonly Color FloorMid = new(0.58f, 0.48f, 0.31f);
	public static readonly Color FloorHigh = new(0.52f, 0.44f, 0.28f);
	public static readonly Color Wall = new(0.42f, 0.31f, 0.24f);
	public static readonly Color BoundaryWall = new(0.35f, 0.25f, 0.18f);
	public static readonly Color CaveFloor = new(0.31f, 0.26f, 0.24f);
	public static readonly Color CaveWall = new(0.24f, 0.21f, 0.20f);
	public static readonly Color CaveShadow = new(0.14f, 0.12f, 0.12f);
	public static readonly Color Ramp = new(0.48f, 0.39f, 0.28f);
	public static readonly Color ChestWood = new(0.46f, 0.27f, 0.12f);
	public static readonly Color ChestMetal = new(0.86f, 0.72f, 0.30f);

	// Characters
	public static readonly Color PlayerBody = new(0.18f, 0.55f, 0.43f);
	public static readonly Color EnemyBody = new(0.63f, 0.32f, 0.18f);
	public static readonly Color EnemyHead = new(0.83f, 0.39f, 0.17f);
	public static readonly Color EnemyGlow = new(0.5f, 0.15f, 0.0f);
	public static readonly Color ItemHeal = new(0.35f, 0.82f, 0.56f);
	public static readonly Color ItemHealMajor = new(0.62f, 0.95f, 0.68f);
	public static readonly Color ItemBomb = new(0.92f, 0.46f, 0.16f);
	public static readonly Color ItemBombMajor = new(1.0f, 0.74f, 0.28f);
	public static readonly Color ItemWard = new(0.52f, 0.82f, 0.94f);
	public static readonly Color ItemPower = new(0.90f, 0.57f, 0.24f);
	public static readonly Color ItemArcane = new(0.80f, 0.66f, 0.96f);
	public static readonly Color EffectInvulnerable = new(0.70f, 0.91f, 0.95f);
	public static readonly Color EffectBulwark = new(0.90f, 0.76f, 0.34f);
	public static readonly Color EffectThorns = new(0.48f, 0.78f, 0.42f);
	public static readonly Color EffectEnraged = new(0.96f, 0.44f, 0.23f);
	public static readonly Color EffectLeech = new(0.41f, 0.92f, 0.56f);
	public static readonly Color HealText = new(0.45f, 1.0f, 0.62f);
	public static readonly Color DamagePlayer = new(1.0f, 0.3f, 0.3f);
	public static readonly Color DamageEnemy = new(1.0f, 0.95f, 0.4f);
	public static readonly Color OutlineBlack = new(0f, 0f, 0f);

	public static void StyleButton(Button btn, int fontSize = 20)
	{
		foreach (var (state, color) in new[]
		{
			("normal", ButtonBg), ("hover", ButtonHover),
			("pressed", Accent), ("disabled", ButtonDisabled),
		})
		{
			var box = new StyleBoxFlat();
			box.BgColor = new Color(color, state == "disabled" ? 0.92f : 0.98f);
			box.BorderColor = state switch
			{
				"hover" => new Color(0.97f, 0.85f, 0.55f, 1.0f),
				"pressed" => new Color(0.98f, 0.88f, 0.60f, 1.0f),
				"disabled" => new Color(TextDisabled, 0.4f),
				_ => new Color(Accent, 0.86f)
			};
			box.SetBorderWidthAll(2);
			box.SetCornerRadiusAll(12);
			box.ShadowColor = new Color(0, 0, 0, 0.34f);
			box.ShadowSize = 6;
			box.ShadowOffset = new Vector2(0, 4);
			box.SetContentMarginAll(18);
			btn.AddThemeStyleboxOverride(state, box);
		}

		btn.AddThemeColorOverride("font_color", TextLight);
		btn.AddThemeColorOverride("font_hover_color", TextLight);
		btn.AddThemeColorOverride("font_pressed_color", BgDark);
		btn.AddThemeColorOverride("font_disabled_color", TextDisabled);
		btn.AddThemeFontSizeOverride("font_size", fontSize);
	}
}
