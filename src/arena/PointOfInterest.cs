using Godot;
using System;
using System.Diagnostics;

public partial class PointOfInterest : Area3D, IInteractable
{
	[Export] public bool MeshOutlineHighlight;
	[Export] public MeshInstance3D? _mesh;
	[Export] public float stopDistance;
	[Export] public Texture2D? cursorIcon;
	[Export] public float outlineWidth;
	[Export] public Color outlineColor;
	protected AdventureCharacter? _character;
	private Node3D? highlight;
	private Node3D? _position;
	bool _keepHighlighted;
	public bool accessed;

	public override void _Ready()
	{
		highlight = GetNode<Node3D>("Highlight");
		if (!MeshOutlineHighlight)
		{
			highlight?.Hide();
		}
		else
		{
			RefreshOutline(false);
		}
		_position = GetNode<Node3D>("Position");
		_character = GetNode<AdventureCharacter>("/root/Map/AdventureCharacter");
	}

	public void Interact(Node user)
	{
		Debug.Assert(_character != null);
		_character.SetPOITarget(_position!.GlobalPosition, stopDistance, this);
		_keepHighlighted = true;
		if (!accessed)
		{
			if (!MeshOutlineHighlight)
			{
				highlight?.Show();
			}
			else
			{
				RefreshOutline(true);
			}
		}
	}

	public void Focus(Node user)
	{
		if (!accessed)
		{
			if (!MeshOutlineHighlight)
			{
				highlight?.Show();
			}
			else
			{
				RefreshOutline(true);
			}
		}
	}

	public void FocusLost(Node user)
	{
		if (!_keepHighlighted)
		{
			if (!MeshOutlineHighlight)
			{
				highlight?.Hide();
			}
			else
			{
				RefreshOutline(false);
			}
		}
	}

	public void UnFocus()
	{
		_keepHighlighted = false;
		if (!MeshOutlineHighlight)
		{
			highlight?.Hide();
		}
		else
		{
			RefreshOutline(false);
		}
		accessed = false;
	}

	public virtual void Access()
	{
		_keepHighlighted = false;
		if (!MeshOutlineHighlight)
		{
			highlight?.Hide();
		}
		else
		{
			RefreshOutline(false);
		}
		accessed = true;
	}

	private void RefreshOutline(bool _enable)
	{
		float thickness = outlineWidth;

		var mesh = _mesh!.Mesh;
		var material = mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
		// WARNING: GODOT stencil API is only Experimental may break on GODOT version's after 4.7.2
        material!.StencilMode = _enable ? BaseMaterial3D.StencilModeEnum.Outline : BaseMaterial3D.StencilModeEnum.Disabled;
        if (!_enable) return;
        material.StencilReference = 1;
        material.StencilColor = outlineColor;
        material.StencilOutlineThickness = thickness;
    }

	public Vector2 GetScreenPosition(Camera3D Camera, Vector3 worldPosition, Control _control)
	{
		Vector2 viewportPosition = Camera!.UnprojectPosition(worldPosition);

		Vector2 scale = _control.GetViewportRect().Size / Camera.GetViewport().GetVisibleRect().Size;

		Vector2 screenPosition = viewportPosition * scale;

		float paddingX = _control.GetViewportRect().Size.X * 0.15f;
		float paddingY = _control.GetViewportRect().Size.Y * 0.15f;

		screenPosition.X = Mathf.Clamp(
			screenPosition.X,
			paddingX,
			_control.GetViewportRect().Size.X - paddingX
		);

		screenPosition.Y = Mathf.Clamp(
			screenPosition.Y,
			paddingY,
			_control.GetViewportRect().Size.Y - paddingY
		);

		return screenPosition;
	}
}
