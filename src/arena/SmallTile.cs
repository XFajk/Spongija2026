using Godot;
using System;
using System.Diagnostics;

public partial class SmallTile : Area3D, IInteractable
{

	private Interactable _interactable;

	private MeshInstance3D _innerSquare;

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		_interactable.InteractionStarted += (Node n) =>
		{
			_innerSquare.Position = new Vector3(0, 0, 1f);
		};

		_interactable.InteractionFinished += (Node n) =>
		{
			_innerSquare.Position = new Vector3(0, 0, 0.01f);
		};

		_interactable.Focused += (Node _) =>
		{
			_innerSquare.Position = new Vector3(0, 0, 0.06f);	
		};	

		_interactable.Unfocused += (Node _) =>
		{
			_innerSquare.Position = new Vector3(0, 0, 0.01f);
		};

		_innerSquare = GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh");
	}

    public void Focus(Node u)
	{
		_interactable.Focus(u);
	}

	public void FocusLost(Node u)
	{
		_interactable.FocusLost(u);
	}

    public void Interact(Node u)
    {
		_interactable.Interact(u);
    }
}
