using Godot;
using System;
using System.Diagnostics;

public partial class SmallTile : Area3D, IInteractable
{

	private Interactable? _interactable;

	private MeshInstance3D? _innerSquare;

	public Vector2I GridPosition { get; set; }

	// Hiding alone keeps the collision, so a hidden tile could still be hovered and focused by the ray
	public bool Enabled
	{
		get => Visible;
		set
		{
			Visible = value;
			// Looked up here rather than in _Ready, because Arena disables tiles before adding them to the tree
			GetNode<CollisionShape3D>("CollisionShape3D").Disabled = !value;
		}
	}

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		Debug.Assert(_interactable != null);
		_innerSquare = GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh");
		Debug.Assert(_innerSquare != null);

		_interactable.InteractionStarted += OnInteractionStarted;


		_interactable.Focused += OnFocused;

		_interactable.Unfocused += OnUnfocused;

		_innerSquare = GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh");
	}

	private void OnInteractionStarted(Node n)
	{
		_innerSquare!.Position = new Vector3(0, 0, 0.01f);
	}	

	private void OnFocused(Node n)
	{
		_innerSquare!.Position = new Vector3(0, 0, 0.06f);
	}

	private void OnUnfocused(Node n)
	{
		_innerSquare!.Position = new Vector3(0, 0, 0.01f);
	}	

    public void Focus(Node u)
	{
		_interactable!.Focus(u);
	}

	public void FocusLost(Node u)
	{
		_interactable!.FocusLost(u);
	}

    public void Interact(Node u)
    {
		_interactable!.Interact(u);
    }
}
