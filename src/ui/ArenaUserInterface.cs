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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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
