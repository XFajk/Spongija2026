using Godot;
using System;
using System.Diagnostics;

public partial class SmallTile : Area3D, IInteractable
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
		if (ArenaRef == null)
			return;

		if (!ArenaRef.IsPlayerTurn)
			return;

		// The red tile under a melee target attacks it from the picked position instead of moving there
		if (ArenaRef.Battle?.GetStackAt(GridPosition) is Stack target)
		{
			ArenaRef.TrySubmitMeleeAttack(target);
			return;
		}

		// For a 2x2 stack this moves to the move preview covering this tile, not to this tile as the anchor
		Raised = false;
		ArenaRef.TrySubmitMoveTo(GridPosition);
	}

	private void OnFocused(Node n)
	{
		Raised = true;
	}

	private void OnUnfocused(Node n)
	{
		Raised = false;
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
