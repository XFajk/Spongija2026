using Godot;
using System;
using System.Diagnostics;

public partial class PlayerCamera : Camera3D
{
	[Export]
	public float InteractionDistance;

	protected RayCast3D? _interactionRayCast;


	public IInteractable? CurrentFocusedNode { get; private set; }

	// Started by a click on it; kept while interact is held and the ray stays on it
	public IInteractable? CurrentInteractedNode { get; private set; }

	// Set by IgnoreCurrentClick until interact is released
	private bool _ignoringClick;

	// The click being held was used by the UI (e.g. it closed the spellbook), so it doesn't start an interaction.
	// Interact counts as released until the button really is.
	public void IgnoreCurrentClick()
	{
		_ignoringClick = true;
	}

	public override void _Ready()
	{
        _interactionRayCast = new RayCast3D
        {
            Name = "InteractionRayCast"
        };
        _interactionRayCast.SetCollisionMaskValue(1, false);
		_interactionRayCast.SetCollisionMaskValue(4, true);
		_interactionRayCast.CollideWithAreas = true;
		AddChild(_interactionRayCast);
	}

	public override void _Process(double delta)
	{
		// A freed node (e.g. a dead unit) can't be called anymore, so drop it without FocusLost
		if (!IsAlive(CurrentFocusedNode))
			CurrentFocusedNode = null;
		if (!IsAlive(CurrentInteractedNode))
			CurrentInteractedNode = null;

		var hovered = GetHoveredInteractable();

		// A locked interaction keeps itself running; nothing else can be interacted with meanwhile,
		// but focus still follows the mouse unless the lock freezes it too
		var locked = Interactable.LockedInteractable;
		if (locked != null)
		{
			if (!locked.FocusLock)
				SetFocus(hovered);
			return;
		}

		if (_ignoringClick && !Input.IsActionPressed("interact"))
			_ignoringClick = false;
		bool interactPressed = Input.IsActionPressed("interact") && !_ignoringClick;

		if (!interactPressed)
			CurrentInteractedNode = null;
		else if (Input.IsActionJustPressed("interact"))
			CurrentInteractedNode = hovered;

		// Moving off the object ends the interaction (unless it locks itself)
		if (CurrentInteractedNode != hovered)
			CurrentInteractedNode = null;

		if (CurrentInteractedNode != null)
		{
			// Nothing stays focused while it's being interacted with
			SetFocus(null);
			CurrentInteractedNode.Interact(this);
		}
		else
		{
			SetFocus(hovered);
		}
	}

	// While the mouse is over a UI control, nothing in the world is hovered, so clicks on buttons (or on the
	// spellbook) never reach the tile or unit behind them. The ray is cleared too, for subclasses that read it.
	private IInteractable? GetHoveredInteractable()
	{
		Debug.Assert(_interactionRayCast != null);
		if (IsMouseOverUI())
		{
			_interactionRayCast.TargetPosition = Vector3.Zero;
			_interactionRayCast.ForceRaycastUpdate();
			return null;
		}

		var mousePosition = GetViewport().GetMousePosition();
		var rayOrigin = ProjectRayOrigin(mousePosition);
		var rayNormal = ProjectRayNormal(mousePosition);

		// ToLocal converts a point, not a direction, so build the world-space end point first
		_interactionRayCast.TargetPosition = _interactionRayCast.ToLocal(rayOrigin + rayNormal * InteractionDistance);
		_interactionRayCast.ForceRaycastUpdate();

		return _interactionRayCast.IsColliding() ? _interactionRayCast.GetCollider() as IInteractable : null;
	}

	// A control that takes the mouse is under it in the window. The ViewportRect showing this camera's view
	// doesn't count: it's the world itself. Controls set to ignore the mouse (labels, stack tags) never count.
	private bool IsMouseOverUI() => GetTree().Root.GuiGetHoveredControl() is Control control and not ViewportRect;

	// Focus and FocusLost are sent only when the focused node changes, not every frame
	private void SetFocus(IInteractable? interactable)
	{
		if (CurrentFocusedNode == interactable)
			return;

		CurrentFocusedNode?.FocusLost(this);
		CurrentFocusedNode = interactable;
		CurrentFocusedNode?.Focus(this);
	}

	private static bool IsAlive(IInteractable? interactable)
	{
		return interactable is not GodotObject node || IsInstanceValid(node);
	}
}
