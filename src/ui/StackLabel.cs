using Godot;

// A stack's name and number of units, drawn over the battle under the stack's feet.
// ArenaUserInterface spawns one per stack (scenes/ui/stack_lable.tscn) and moves it every frame.
public partial class StackLabel : Control
{
	[Export]
	public Texture2D? PlayerTagTexture { get; set; } = GD.Load<Texture2D>("uid://5pn33yemlswx");

	[Export]
	public Texture2D? ComputerTagTexture { get; set; } = GD.Load<Texture2D>("uid://dsiugxguwvrim");

	// Added to the point under the stack's feet, in pixels. The label's top center (name and tag together) goes there.
	[Export]
	public Vector2 ScreenOffset { get; set; } = new(0, 4);

	// Relative to the label (names are at 0); ArenaUserInterface puts its other layers above it
	public const int TagZIndex = 1;

	public Stack? Stack { get; private set; }

	private Label? _nameLabel;
	private TextureRect? _numberTag;
	private Label? _quantityLabel;

	// Shown only when it changes; null until the first Refresh
	private uint? _shownQuantity;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("StackName");
		_numberTag = GetNode<TextureRect>("StackName/NumberTag");
		_quantityLabel = GetNode<Label>("StackName/NumberTag/NumberOfUnitsInStack");
		// Every tag draws over every stack's name, so overlapping labels never hide a number
		_numberTag.ZIndex = TagZIndex;
		// The labels lie over the battle, so clicks and hovers must go through them to the units and tiles
		IgnoreMouse(this);
	}

	// Called once, right after the label is added to the tree. The side picks the tag's image.
	public void Setup(Stack stack, bool isPlayerStack)
	{
		Stack = stack;
		_nameLabel!.Text = stack.Unit.Name;
		// Shrinks the name's box to its text, so the tag, anchored to its right edge, follows the name right away
		_nameLabel.ResetSize();
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

	// Puts the top center of the name and tag together, plus ScreenOffset, on the canvas point
	public void PlaceAt(Vector2 canvasPoint)
	{
		// Both rects relative to this control; the tag is a child of the name
		Rect2 nameRect = _nameLabel!.GetRect();
		Rect2 tagRect = _numberTag!.GetRect();
		tagRect.Position += nameRect.Position;
		Rect2 bounds = nameRect.Merge(tagRect);
		GlobalPosition = canvasPoint + ScreenOffset - new Vector2(bounds.GetCenter().X, bounds.Position.Y);
	}

	private static void IgnoreMouse(Node node)
	{
		if (node is Control control)
			control.MouseFilter = MouseFilterEnum.Ignore;
		foreach (Node child in node.GetChildren())
			IgnoreMouse(child);
	}
}
