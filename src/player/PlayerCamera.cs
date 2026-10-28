using Godot;
using System;
using System.Diagnostics;

public partial class PlayerCamera : Camera3D
{
	[Export]
	public float InteractionDistance;

	private RayCast3D? _interactionRayCast;


	public IInteractable? CurrentFocusedNode { get; private set; }

	// Started by a click on it; kept while interact is held and the ray stays on it
	public IInteractable? CurrentInteractedNode { get; private set; }

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

		if (!Input.IsActionPressed("interact"))
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

	private IInteractable? GetHoveredInteractable()
	{
		Debug.Assert(_interactionRayCast != null);
		var mousePosition = GetViewport().GetMousePosition();
		var rayOrigin = ProjectRayOrigin(mousePosition);
		var rayNormal = ProjectRayNormal(mousePosition);

		// ToLocal converts a point, not a direction, so build the world-space end point first
		_interactionRayCast.TargetPosition = _interactionRayCast.ToLocal(rayOrigin + rayNormal * InteractionDistance);
		_interactionRayCast.ForceRaycastUpdate();

		return _interactionRayCast.IsColliding() ? _interactionRayCast.GetCollider() as IInteractable : null;
	}

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
