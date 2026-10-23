using Godot;
using System;

public partial class PointOfInterest : Area3D, IInteractable
{
	[Export] public float stopDistance;
	AdventureCharacter _character;
	private MeshInstance3D highlight;
	private Interactable _interactable;

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		highlight = GetNode<MeshInstance3D>("Highlight");
		highlight.Hide();
		_character = GetNode<AdventureCharacter>("/root/Map/AdventureCharacter");
	}

	public void Interact(Node user)
	{
		_character.SetPOITarget(GlobalPosition, stopDistance, this);
	}

	public void Focus(Node user)
	{
		highlight.Show();
	}

	public void FocusLost(Node user)
	{
		highlight.Hide();
	}

	public void Access()
	{
		
	}
}
