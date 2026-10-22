using Godot;
using System;
using System.Diagnostics;

// One tile under a 2x2 stack, replacing the 4 small tiles it covers. A child of the stack's UnitNode,
// so it moves with it. GridPosition is the stack's anchor.
public partial class DoubleTile : Area3D, IInteractable
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
			GetNode<CollisionShape3D>("CollisionShape3D").Disabled = !value;
		}
	}

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		Debug.Assert(_interactable != null);
		_innerSquare = GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh");
		Debug.Assert(_innerSquare != null);

		_interactable.Focused += OnFocused;

		_interactable.Unfocused += OnUnfocused;
	}

	private void OnFocused(Node n)
	{
		_innerSquare!.Position = new Vector3(0, 0, 0.06f);
	}

	private void OnUnfocused(Node n)
	{
		_innerSquare!.Position = new Vector3(0, 0, 0.01f);
	}

    public void Interact(Node user)
    {
		_interactable!.Interact(user);
    }

    public void Focus(Node user)
    {
		_interactable!.Focus(user);
    }

    public void FocusLost(Node user)
    {
		_interactable!.FocusLost(user);
    }
}
