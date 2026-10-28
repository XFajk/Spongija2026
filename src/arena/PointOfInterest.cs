using Godot;
using System;
using System.Diagnostics;

public partial class PointOfInterest : Area3D, IInteractable
{
	[Export] public poiType type;
	[Export] public float stopDistance;
	[Export] public Texture2D? cursorIcon;
	AdventureCharacter? _character;
	private MeshInstance3D? highlight;
	bool _keepHighlighted;
	bool accessed;

	public override void _Ready()
	{
		highlight = GetNode<MeshInstance3D>("Highlight");
		highlight!.Hide();
		_character = GetNode<AdventureCharacter>("/root/Map/AdventureCharacter");
	}

	public void Interact(Node user)
	{
		Debug.Assert(_character != null);
		_character.SetPOITarget(GlobalPosition, stopDistance, this);
		_keepHighlighted = true;
		if (!accessed)
		{
			highlight?.Show();
		}
	}

	public void Focus(Node user)
	{
		if (!accessed)
		{
			highlight?.Show();
		}
	}

	public void FocusLost(Node user)
	{
		if (!_keepHighlighted)
		{
			highlight?.Hide();
		}
	}

	public void UnFocus()
	{
		_keepHighlighted = false;
		highlight?.Hide();
		accessed = false;
	}

	public void Access()
	{
		_keepHighlighted = false;
		highlight?.Hide();
		accessed = true;
	}
}

public enum poiType
{
	resource,
	fight
}
