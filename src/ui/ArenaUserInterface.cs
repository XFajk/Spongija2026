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

	public override void _ExitTree()
	{
		// Never leave the game with the mouse hidden
		HideMeleeCursor();
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
		Input.MouseMode = Input.MouseModeEnum.Hidden;
	}

	public void HideMeleeCursor()
	{
		if (_meleeCursor == null || !_meleeCursor.Visible)
			return;
		_meleeCursor.Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
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
