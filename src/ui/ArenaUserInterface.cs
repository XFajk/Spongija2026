using Godot;
using System;

public partial class ArenaUserInterface : Control
{

	public Arena? arena;	
	public ArenaCamera? arenaCamera;

	private Button? _startBattleButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_startBattleButton = GetNode<Button>("SetupInterface/StartBattle");
		_startBattleButton!.Pressed += OnStartBattleButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnStartBattleButtonPressed()
	{
		arena?.EmitSignal(Arena.SignalName.SetupPlacementPhaseCompleted);
	}
}
