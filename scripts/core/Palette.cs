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
	public static readonly Color Wall = new(0.42f, 0.31f, 0.24f);
	public static readonly Color BoundaryWall = new(0.35f, 0.25f, 0.18f);

	// Characters
	public static readonly Color PlayerBody = new(0.18f, 0.55f, 0.43f);
	public static readonly Color EnemyBody = new(0.63f, 0.32f, 0.18f);
	public static readonly Color EnemyHead = new(0.83f, 0.39f, 0.17f);
	public static readonly Color EnemyGlow = new(0.5f, 0.15f, 0.0f);
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
			box.BgColor = color;
			box.SetCornerRadiusAll(8);
			box.SetContentMarginAll(16);
			btn.AddThemeStyleboxOverride(state, box);
		}

		btn.AddThemeColorOverride("font_color", TextLight);
		btn.AddThemeColorOverride("font_hover_color", TextLight);
		btn.AddThemeColorOverride("font_pressed_color", BgDark);
		btn.AddThemeColorOverride("font_disabled_color", TextDisabled);
		btn.AddThemeFontSizeOverride("font_size", fontSize);
	}
}
