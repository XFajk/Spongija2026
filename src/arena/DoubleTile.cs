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

	// Set by Arena right after instantiating, before the tile enters the tree, so it's valid in _Ready
	public Arena? ArenaRef { get; set; } = null;

	// Materials swapped in while the tile is red: under a melee target or the picked attack position. Shared .tres files.
	[Export]
	public Material? MeleeOuterMaterial { get; set; }

	[Export]
	public Material? MeleeInnerMaterial { get; set; }

	// The scene's own materials, put back when the tile stops being a melee attack tile
	private Material? _outerMaterial;
	private Material? _innerMaterial;
	private bool _isMeleeAttackTile = false;

	public bool IsMeleeAttackTile
	{
		get => _isMeleeAttackTile;
		set
		{
			if (_isMeleeAttackTile == value)
				return;
			_isMeleeAttackTile = value;

			var outer = GetNode<MeshInstance3D>("SmallTileMesh");
			var inner = GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh");
			// Remembered on the first swap, so it doesn't matter whether _Ready ran yet
			_outerMaterial ??= outer.MaterialOverride;
			_innerMaterial ??= inner.MaterialOverride;
			outer.MaterialOverride = value ? MeleeOuterMaterial : _outerMaterial;
			inner.MaterialOverride = value ? MeleeInnerMaterial : _innerMaterial;
		}
	}

	// The focused look: the inner square lifted. Also used for the picked melee attack position.
	public bool Raised
	{
		get => GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh").Position.Z > 0.01f;
		set => GetNode<MeshInstance3D>("SmallTileMesh/InnerSquareMesh").Position = new Vector3(0, 0, value ? 0.06f : 0.01f);
	}

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

		_interactable.InteractionStarted += OnInteractionStarted;

		_interactable.Focused += OnFocused;

		_interactable.Unfocused += OnUnfocused;
	}

	private void OnFocused(Node n)
	{
		Raised = true;
	}

	private void OnUnfocused(Node n)
	{
		Raised = false;
	}

	// A 2x2 melee target's double tile attacks it from the picked position
	// The move preview moves the current 2x2 stack there
	private void OnInteractionStarted(Node n)
	{
		if (ArenaRef == null)
			return;

		if (ArenaRef.MovePreview == this)
		{
			ArenaRef.TrySubmitMoveTo(GridPosition);
			return;
		}

		if (ArenaRef.Battle?.GetStackAt(GridPosition) is Stack target && !ArenaRef.TrySubmitMeleeAttack(target))
			ArenaRef.TrySubmitRangedAttack(target);
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
