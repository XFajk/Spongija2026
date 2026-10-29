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

	public Stack? Stack { get; private set; }

	private TextureRect? _numberTag;
	private Label? _quantityLabel;
	private Label? _previewLabel;

	// Shown only when it changes; null until the first Refresh
	private uint? _shownQuantity;

	public override void _Ready()
	{
		_numberTag = GetNode<TextureRect>("NumberTag");
		_quantityLabel = GetNode<Label>("NumberTag/NumberOfUnitsInStack");

		// Made here rather than in the scene, so the scene stays only the tag. Centered under the tag.
		Rect2 tag = _numberTag.GetRect();
		_previewLabel = new Label
		{
			Name = "Preview",
			HorizontalAlignment = HorizontalAlignment.Center,
			Position = new Vector2(tag.Position.X, tag.End.Y),
			Size = new Vector2(tag.Size.X, PreviewFontSize + 8),
			LabelSettings = new LabelSettings { FontSize = PreviewFontSize, OutlineSize = 8, OutlineColor = Colors.Black },
			Visible = false,
		};
		AddChild(_previewLabel);

		// The labels lie over the battle, so clicks and hovers must go through them to the units and tiles
		IgnoreMouse(this);
	}

	// Shows what the hovered action would do to this stack: -kills in red, +revives in green, or nothing (0, 0)
	public void ShowPreview(int unitsKilled, int unitsRevived)
	{
		if (_previewLabel == null)
			return;
		if (unitsKilled > 0)
		{
			_previewLabel.Text = $"-{unitsKilled}";
			_previewLabel.LabelSettings.FontColor = KillPreviewColor;
		}
		else if (unitsRevived > 0)
		{
			_previewLabel.Text = $"+{unitsRevived}";
			_previewLabel.LabelSettings.FontColor = RevivePreviewColor;
		}
		_previewLabel.Visible = unitsKilled > 0 || unitsRevived > 0;
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
