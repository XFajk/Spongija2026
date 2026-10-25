using Godot;
using System;

public partial class ArenaUserInterface : Control
{

	public Arena? arena;	
	public ArenaCamera? arenaCamera;

	private Control? _setupInterface;
	private Control? _battleInterface;

	// UI elements for the setup  interfaces
	private Button? _startBattleButton;

	// UI elements for the battle interface
	private Button? _useUnitSpellButton;
	private Button? _waitButton;

	private Button? _useHeroSpellButton;

	public const string MeleeCursorTexture = "uid://c21n6vo2a3nt4";

	// Scale of the sword that replaces the mouse cursor while picking where to melee from (the image is 256x256).
	[Export]
	public float MeleeCursorScale { get; set; } = 0.35f;

	private Sprite2D? _meleeCursor;

	public const string RangedCursorTexture = "uid://nidiayrbf26b";

	// Scale of the arrow that replaces the mouse cursor over an enemy that can be shot (the image is 256x256).
	[Export]
	public float RangedCursorScale { get; set; } = 0.35f;

	// The pixel of the arrow image that sits on the mouse position: the arrowhead's tip.
	[Export]
	public Vector2 RangedCursorHotspot { get; set; } = new(168, 172);

	private Sprite2D? _rangedCursor;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// First, so a broken node path below can't keep the cursor from existing
		_meleeCursor = new Sprite2D
		{
			Name = "MeleeCursor",
			Texture = GD.Load<Texture2D>(MeleeCursorTexture),
			Scale = Vector2.One * MeleeCursorScale,
			Visible = false,
		};
		AddChild(_meleeCursor);

		_rangedCursor = new Sprite2D
		{
			Name = "RangedCursor",
			Texture = GD.Load<Texture2D>(RangedCursorTexture),
			Scale = Vector2.One * RangedCursorScale,
			Centered = false,
			Offset = -RangedCursorHotspot,
			Visible = false,
		};
		AddChild(_rangedCursor);

		_setupInterface = GetNode<Control>("SetupInterface");
		_battleInterface = GetNode<Control>("BattleInterface");

		_startBattleButton = GetNode<Button>("SetupInterface/StartBattle");
		_startBattleButton!.Pressed += OnStartBattleButtonPressed;

		_useUnitSpellButton = GetNode<Button>("BattleInterface/BattleActions/UnitActions/UseSpell");
		_useUnitSpellButton!.Pressed += OnUseUnitSpellButtonPressed;

		_waitButton = GetNode<Button>("BattleInterface/BattleActions/UnitActions/Wait");
		_waitButton!.Pressed += OnWaitButtonPressed;

		_useHeroSpellButton = GetNode<Button>("BattleInterface/BattleActions/UseHeroSpell");
		_useHeroSpellButton!.Pressed += OnUseHeroSpellButtonPressed;

		if (arena is not Arena a)
		{
			return;
		}

		ShowSetupInterface();
		a.SetupPlacementPhaseCompleted += ShowBattleInterface;
	}

	public override void _Process(double delta)
	{
		// The arrow follows the mouse, unlike the sword, which Arena places on the picked tile
		if (_rangedCursor != null && _rangedCursor.Visible)
			_rangedCursor.GlobalPosition = GetGlobalMousePosition();
	}

	public override void _ExitTree()
	{
		// Never leave the game with the mouse hidden
		HideMeleeCursor();
		HideRangedCursor();
	}

	// Replaces the mouse cursor with the sword: the mouse is hidden where it is (it still moves and hovers),
	// and the sword is drawn at the canvas position, rotated clockwise by the angle from pointing up.
	public void ShowMeleeCursor(Vector2 position, float rotation)
	{
		if (_meleeCursor == null)
			return;
		_meleeCursor.GlobalPosition = position;
		_meleeCursor.Rotation = rotation;
		_meleeCursor.Visible = true;
		UpdateMouseMode();
	}

	public void HideMeleeCursor()
	{
		if (_meleeCursor == null || !_meleeCursor.Visible)
			return;
		_meleeCursor.Visible = false;
		UpdateMouseMode();
	}

	// Replaces the mouse cursor with the arrow, its tip on the mouse position.
	public void ShowRangedCursor()
	{
		if (_rangedCursor == null)
			return;
		_rangedCursor.GlobalPosition = GetGlobalMousePosition();
		_rangedCursor.Visible = true;
		UpdateMouseMode();
	}

	public void HideRangedCursor()
	{
		if (_rangedCursor == null || !_rangedCursor.Visible)
			return;
		_rangedCursor.Visible = false;
		UpdateMouseMode();
	}

	// The mouse is hidden while either cursor stands in for it
	private void UpdateMouseMode()
	{
		bool replaced = (_meleeCursor?.Visible ?? false) || (_rangedCursor?.Visible ?? false);
		Input.MouseMode = replaced ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
	}

	private void ShowSetupInterface()
	{
		_setupInterface!.Visible = true;
		_battleInterface!.Visible = false;
	}

	private void ShowBattleInterface()
	{
		_setupInterface!.Visible = false;
		_battleInterface!.Visible = true;
	}


	private void OnUseUnitSpellButtonPressed()
	{
	}

	private void OnWaitButtonPressed()
	{
	}

	private void OnUseHeroSpellButtonPressed()
	{
	}

	private void OnStartBattleButtonPressed()
	{
		arena?.EmitSignal(Arena.SignalName.SetupPlacementPhaseCompleted);
	}
}
