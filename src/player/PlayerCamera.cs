using Godot;
using System;
using System.Diagnostics;

public partial class PlayerCamera : Camera3D
{	
	[Export]
	public float InteractionDistance;

	private RayCast3D? _interactionRayCast;

	private IInteractable? _currentFocusedNode;

	public override void _Ready()
	{
		_interactionRayCast = GetNode<RayCast3D>("InteractionRayCast");
	}	

	public override void _Process(double delta)
	{
		Debug.Assert(_interactionRayCast != null);
		var mousePosition = GetViewport().GetMousePosition();	
		var rayOrigin = ProjectRayOrigin(mousePosition);
		var rayNormal = ProjectRayNormal(mousePosition);

		// ToLocal converts a point, not a direction, so build the world-space end point first
		_interactionRayCast.TargetPosition = _interactionRayCast.ToLocal(rayOrigin + rayNormal * InteractionDistance);
		_interactionRayCast.ForceRaycastUpdate();

		if (_interactionRayCast.IsColliding())
		{
			var collider = _interactionRayCast.GetCollider();		

			if (collider is IInteractable interactable)
			{
				if (_currentFocusedNode != interactable)
				{
					_currentFocusedNode?.FocusLost(this);
					_currentFocusedNode = interactable;
				}

				interactable.Focus(this);
				if (Input.IsActionJustPressed("interact"))
					interactable.Interact(this);
			}
		}
	}
}
