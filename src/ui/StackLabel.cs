using Godot;

// A stack's number of units on a tag, drawn over the stack's feet so it stays on the model, with a preview under it
// of what the hovered attack or spell would do to the stack.
// ArenaUserInterface spawns one per stack (scenes/ui/stack_lable.tscn) and moves it every frame.
public partial class StackLabel : Control
{
	[Export]
	public Texture2D? PlayerTagTexture { get; set; } = GD.Load<Texture2D>("uid://5pn33yemlswx");

	[Export]
	public Texture2D? ComputerTagTexture { get; set; } = GD.Load<Texture2D>("uid://dsiugxguwvrim");

	// Added to the point at the stack's feet, in pixels. The tag is centered on this control's origin in the scene,
	// so it's centered there.
	[Export]
	public Vector2 ScreenOffset { get; set; } = Vector2.Zero;

	// The preview under the tag (ShowPreview): units the hovered action would kill, or bring back
	[ExportGroup("Preview")]
	[Export]
	public Color KillPreviewColor { get; set; } = new(1f, 0.3f, 0.25f);

	[Export]
	public Color RevivePreviewColor { get; set; } = new(0.4f, 1f, 0.45f);

	[Export]
	public int PreviewFontSize { get; set; } = 26;

	// The second line, the health the action takes or gives: damage, healing
	[Export]
	public Color DamagePreviewColor { get; set; } = new(1f, 0.75f, 0.55f);

	[Export]
	public Color HealPreviewColor { get; set; } = new(0.6f, 1f, 0.65f);

	[Export]
	public int HealthPreviewFontSize { get; set; } = 20;

	// Wider than the tag, so "1200 damage" fits on one line; centered under it
	private const float PreviewWidth = 260f;

	public Stack? Stack { get; private set; }

	private TextureRect? _numberTag;
	private Label? _quantityLabel;
	// Units killed or brought back
	private Label? _previewLabel;
	// Health taken or given: shows even when no unit dies, so a hit or a heal on the top unit can be seen
	private Label? _healthPreviewLabel;

	// Shown only when it changes; null until the first Refresh
	private uint? _shownQuantity;

	public override void _Ready()
	{
		_numberTag = GetNode<TextureRect>("NumberTag");
		_quantityLabel = GetNode<Label>("NumberTag/NumberOfUnitsInStack");

		// Made here rather than in the scene, so the scene stays only the tag. Centered under the tag, units first.
		Rect2 tag = _numberTag.GetRect();
		var previews = new VBoxContainer
		{
			Name = "Previews",
			Position = new Vector2(tag.GetCenter().X - PreviewWidth / 2f, tag.End.Y),
			Size = new Vector2(PreviewWidth, 0),
		};
		previews.AddThemeConstantOverride("separation", 0);
		// Above every stack's tag (all at 0), so another stack's tag never covers the numbers; still under the
		// interfaces, whose ZIndex is the same but which come later in the tree (ArenaUserInterface.SetDrawOrder)
		previews.ZIndex = 1;
		AddChild(previews);

		_previewLabel = MakePreviewLabel("Preview", PreviewFontSize);
		previews.AddChild(_previewLabel);
		_healthPreviewLabel = MakePreviewLabel("HealthPreview", HealthPreviewFontSize);
		previews.AddChild(_healthPreviewLabel);

		// The labels lie over the battle, so clicks and hovers must go through them to the units and tiles
		IgnoreMouse(this);
	}

	private static Label MakePreviewLabel(string name, int fontSize) => new()
	{
		Name = name,
		HorizontalAlignment = HorizontalAlignment.Center,
		CustomMinimumSize = new Vector2(PreviewWidth, 0),
		LabelSettings = new LabelSettings { FontSize = fontSize, OutlineSize = 8, OutlineColor = Colors.Black },
		Visible = false,
	};

	// Shows what the hovered action would do to this stack, or nothing for ActionPreview.None: the units it kills
	// (-N, red) or brings back (+N, green), and under them the health it takes (N damage) or gives (+N health).
	// A heal on a stack whose top unit is unhurt says so, since it would do nothing.
	public void ShowPreview(ActionPreview preview)
	{
		if (_previewLabel == null || _healthPreviewLabel == null)
			return;

		if (preview.UnitsKilled > 0)
			SetPreview(_previewLabel, $"-{preview.UnitsKilled}", KillPreviewColor);
		else if (preview.UnitsRevived > 0)
			SetPreview(_previewLabel, $"+{preview.UnitsRevived}", RevivePreviewColor);
		else
			_previewLabel.Visible = false;

		if (preview.Damage > 0)
			SetPreview(_healthPreviewLabel, $"{preview.Damage} damage", DamagePreviewColor);
		else if (preview.Healing > 0)
			SetPreview(_healthPreviewLabel, $"+{preview.Healing} health", HealPreviewColor);
		else if (preview.IsHeal)
			SetPreview(_healthPreviewLabel, "Already at full health", HealPreviewColor);
		else
			_healthPreviewLabel.Visible = false;
	}

	private static void SetPreview(Label label, string text, Color color)
	{
		label.Text = text;
		label.LabelSettings.FontColor = color;
		label.Visible = true;
	}

	// Called once, right after the label is added to the tree. The side picks the tag's image.
	public void Setup(Stack stack, bool isPlayerStack)
	{
		Stack = stack;
		_numberTag!.Texture = isPlayerStack ? PlayerTagTexture : ComputerTagTexture;
		Refresh();
	}

	// Shows the stack's current number of units
	public void Refresh()
	{
		if (Stack == null || Stack.Quantity == _shownQuantity)
			return;
		_shownQuantity = Stack.Quantity;
		_quantityLabel!.Text = _shownQuantity.ToString();
	}

	// Centers the tag, plus ScreenOffset, on the canvas point
	public void PlaceAt(Vector2 canvasPoint)
	{
		GlobalPosition = canvasPoint + ScreenOffset;
	}

	private static void IgnoreMouse(Node node)
	{
		if (node is Control control)
			control.MouseFilter = MouseFilterEnum.Ignore;
		foreach (Node child in node.GetChildren())
			IgnoreMouse(child);
	}
}

// What the hovered attack or spell would do to one stack (Arena.UpdateActionPreview), for its label. Damage and
// Healing are what really lands, capped like SpellEstimate's. IsHeal: the action heals, even if it would heal nothing.
public readonly record struct ActionPreview(int Damage = 0, int UnitsKilled = 0, int Healing = 0, int UnitsRevived = 0,
	bool IsHeal = false)
{
	public static readonly ActionPreview None = new();

	public static ActionPreview From(SpellEstimate estimate) => new(estimate.Damage, estimate.UnitsKilled,
		estimate.Healing, estimate.Revives, IsHeal: estimate.PotentialHealing > 0);
}
