using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public partial class AdventureCharacter : CharacterBody3D, IInteractable, ISavable
{
	 [Export]
	public string SaveId { get; set; } = "player";
	[Export] public float stopDistance;
	[Export] public float Speed = 5.0f;
	[Export] public float RotationSpeed = 5.0f;
	public Vector3 TargetPosition;
	private NavigationAgent3D? agent;
	[Export] PackedScene? point;
	[Export] PackedScene? finalPoint;
	[Export] Node3D? container;
	[Export] float pointsMinDistance;
	List<Node3D> points = new List<Node3D>();
	public PointOfInterest? poi;

	public CharacterPanel? inventoryPanel;
	public Inventory? inventory;
	public bool canMove;
	public bool movingToPoint;
	[Export] PauseMenu? pauseWindow;
	[Export] SaveBrowser? _saves;
	[Export] UnitAnimationManager? animationManager;

	public override void _Ready()
	{
		agent = GetNode<NavigationAgent3D>("NavigationAgent3D");

		agent.PathDesiredDistance = 0.5f;
		agent.TargetDesiredDistance = 0.5f;

		inventoryPanel = GetNode<CharacterPanel>("/root/Map/CanvasLayer/Canvas/Character");
		Button inventoryButtonClose = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Categories/Close");
		inventoryButtonClose.Pressed += HidePanel;
		Button resumeButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/PauseMenu/MarginContainer/VBoxContainer/Back");
		resumeButton.Pressed += Resume;

		Button saveButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/PauseMenu/MarginContainer/VBoxContainer/Save");
		saveButton.Pressed += SaveSelect;

		Button loadButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/PauseMenu/MarginContainer/VBoxContainer/Load");
		loadButton.Pressed += LoadSelect;

		Button quitButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/PauseMenu/MarginContainer/VBoxContainer/Quit");
		quitButton.Pressed += Quit;

		Button cancelSave = GetNode<Button>("/root/Map/CanvasLayer/Canvas/select_file/CancelSaveMenu");
		cancelSave.Pressed += CancelSaveMenu;

		canMove = true;
		inventory = GetNode<Inventory>("Inventory");

		CallDeferred(nameof(SetupNavigation));
	}

	private async void SetupNavigation()
	{
		await ToSignal(
			GetTree(),
			SceneTree.SignalName.PhysicsFrame
		);
	}

	public void SetTarget(Vector3 pos)
	{
		if (poi != null)
		{
			poi.UnFocus();
		}

		poi = null;
		agent!.TargetDesiredDistance = stopDistance;
		TargetPosition = pos;
		agent.TargetPosition = TargetPosition;
		ResetPoints();
		DisplayPathPoints();
		movingToPoint = true;
	}

	public void SetPOITarget(Vector3 pos, float distance, PointOfInterest _point)
	{
		if (!canMove)
			return;

		if (poi != null && poi != _point)
		{
			poi.UnFocus();
		}

		poi = _point;
		agent!.TargetDesiredDistance = distance;
		TargetPosition = pos;
		agent.TargetPosition = TargetPosition;
		ResetPoints();
		DisplayPathPoints();
		movingToPoint = false;
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Escape))
		{
			if (!GetTree().Paused)
			{
				Pause();
			}
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		if (agent!.IsNavigationFinished())
		{
			if (!TutorialChecklist.inventoryShown && movingToPoint)
			{
				GetNode<Tutorial>("/root/Map/Tutorial").ShowInventory();
			}

			movingToPoint = false;
			Velocity = Vector3.Zero;
			MoveAndSlide();
			SetRunning(false);

			if (points.Count > 0)
			{
				ResetPoints();
			}

			if (poi != null && !poi.accessed)
			{
				poi.Access();
			}

			return;
		}

		Vector3 nextPosition = agent.GetNextPathPosition();
		Vector3 direction = GlobalPosition.DirectionTo(nextPosition);

		if (direction.LengthSquared() > 0.001f)
		{
			float targetAngle = Mathf.Atan2(
				direction.X,
				direction.Z
			);

			Rotation = new Vector3(
				Rotation.X,
				Mathf.LerpAngle(
					Rotation.Y,
					targetAngle,
					RotationSpeed * (float)delta
				),
				Rotation.Z
			);
		}

		Velocity = direction * Speed;

		MoveAndSlide();
		SetRunning(true);

		for (int i = 0; i < points.Count; i++)
		{
			if (GlobalPosition.DistanceTo(points[i].GlobalPosition) <= 0.5f)
			{
				points[i].QueueFree();
				points.RemoveAt(i);
			}
		}
	}

	void DisplayPathPoints()
	{
		Rid map = GetWorld3D().NavigationMap;

		Vector3[] path = NavigationServer3D.MapGetPath(
			map,
			GlobalPosition,
			TargetPosition,
			true
		);

		float distanceToNext = 0f;

		for (int i = 0; i < path.Length - 1; i++)
		{
			Vector3 start = path[i];
			Vector3 end = path[i + 1];

			float length = start.DistanceTo(end);

			if (length < 0.001f)
				continue;

			while (distanceToNext <= length)
			{
				float t = distanceToNext / length;
				Vector3 position = start.Lerp(end, t);

				Node3D _point = point!.Instantiate<Node3D>();
				_point.Hide();
				container!.AddChild(_point);
				_point.GlobalPosition = position;

				if (_point.GlobalPosition.DistanceTo(TargetPosition) < agent!.TargetDesiredDistance && agent!.TargetDesiredDistance != stopDistance)
				{
					_point.QueueFree();
				}
				else
				{
					_point.Show();
					points.Add(_point);
				}

				distanceToNext += pointsMinDistance;
			}

			distanceToNext -= length;

			if (agent!.TargetDesiredDistance == stopDistance)
			{
				var endMaterial = new StandardMaterial3D();
				endMaterial.AlbedoColor = Colors.Yellow;

				if (points[points.Count - 1].GlobalPosition.DistanceTo(path[path.Length - 1]) <= 0.5f)
				{
					Node3D _point = finalPoint!.Instantiate<Node3D>();
					container!.AddChild(_point);
					_point.GlobalPosition = path[path.Length - 1];
					points[points.Count - 1].QueueFree();
					points[points.Count - 1] = _point;
				}
				else
				{
					Node3D _point = finalPoint!.Instantiate<Node3D>();
					container!.AddChild(_point);
					_point.GlobalPosition = path[path.Length -1];
					points.Add(_point);
				}
			}
		}
	}

	// Plays the model's run animation (UnitAnimationManager's state machine reads IsRunning)
	void SetRunning(bool running)
	{
		if (animationManager != null)
		{
			animationManager.IsRunning = running;
		}
	}

	void ResetPoints()
	{
		for (int i = 0; i < points.Count; i++)
		{
			points[i].QueueFree();
		}

		points = new List<Node3D>();
	}

	void ReduceClosePoints()
	{
		for (int i = 1; i < points.Count - 1; i++)
		{
			if (points[i].GlobalPosition.DistanceTo(points[i - 1].GlobalPosition) < pointsMinDistance || points[i].GlobalPosition.DistanceTo(points[i + 1].GlobalPosition) < pointsMinDistance)
			{
				points[i].QueueFree();
				points.RemoveAt(i);
				i--;
			}
		}
	}

	public void Focus(Node user)
	{
		
   
	}

	public void FocusLost(Node user)
	{
		
	}

	public void Interact(Node user)
	{
		inventoryPanel!.Show();
		canMove = false;
		if (!TutorialChecklist.inventoryExplained)
		{
			GetNode<Tutorial>("/root/Map/Tutorial").ExplainInventory();
		}
	}

	public void HidePanel()
	{
		inventoryPanel?.ResetSplitting();
		inventoryPanel?.ResetCombining();
		inventoryPanel?.ResetEquipping();
		inventoryPanel!.Hide();
		canMove = true;
	}

	public void ForceStop()
	{
		for (int i = 0; i < points.Count; i++)
		{
			points[i].QueueFree();
			points.RemoveAt(i);
			agent!.TargetPosition = GlobalPosition;
		}
	}

	public void EnterFight()
	{
		GetTree().Paused = false;
		GetNode<BattleStarter>("BattleStarter").EnterBattle(inventory!.Spells!, inventory!.Units!, inventory!.Mana);
	}

	public void SetupEnemyArmy(Godot.Collections.Array<StackResource> units, Godot.Collections.Array<SpellAbility> spells)
	{
		GetNode<BattleStarter>("BattleStarter").SetupEnemyArmy(units, spells);
	}

	public void SetupDifficulty(EnemyStrategy enemyStrategy)
	{
		GetNode<BattleStarter>("BattleStarter").SetupDifficulty(enemyStrategy);
	}

	public void Pause()
	{
		HidePanel();
		pauseWindow!.Show();
		pauseWindow.Setup();
		GetTree().Paused = true;
	}

	public void Resume()
	{
		pauseWindow!.Hide();
		GetTree().Paused = false;
	}

	public void SaveSelect()
	{
		pauseWindow!.Hide();
		_saves!.Show();
		_saves!.Setup(true);
	}

	public void LoadSelect()
	{
		pauseWindow!.Hide();
		_saves!.Show();
		_saves!.Setup(false);
	}

	public void CancelSaveMenu()
	{
		pauseWindow!.Hide();
		_saves!.Hide();
		GetTree().Paused = false;
	}

	public void Quit()
	{
		pauseWindow!.Hide();
		GetTree().Paused = false;
		_saves!.manager!.SaveGame(CurrentSave.CurrentSaveIndex);

		Control menu = GD.Load<PackedScene>("uid://1cwqotqa3spu").Instantiate<Control>();
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, menu);
	}

	//Save
	public Godot.Collections.Dictionary<string, Variant> Save()
	{
		return new()
		{
			{ "position_x", GlobalPosition.X },
			{ "position_y", GlobalPosition.Y },
			{ "position_z", GlobalPosition.Z }
		};
	}

	public void Load(Godot.Collections.Dictionary data)
	{
		GlobalPosition = new Vector3(
		(float)data["position_x"].AsDouble(),
		(float)data["position_y"].AsDouble(),
		(float)data["position_z"].AsDouble()
   		);
	}
}
