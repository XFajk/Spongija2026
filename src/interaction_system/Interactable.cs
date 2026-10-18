using System.Reflection.Metadata;
using Godot;

public interface IInteractable
{
	void Interact(Node user);
	void Focus(Node user);
	void FocusLost(Node user);
}

[GlobalClass]
public partial class Interactable : Node
{

	[Signal]
	public delegate void InteractingEventHandler(Node user);
	[Signal]
	public delegate void PhysicsInteractingEventHandler(Node user);
	[Signal]
	public delegate void InteractionStartedEventHandler(Node user);
	[Signal]
	public delegate void InteractionFinishedEventHandler(Node user);

	[Signal]
	public delegate void FocusedEventHandler(Node user);
    [Signal]
	public delegate void UnfocusedEventHandler(Node user);

	public bool InteractionLock = false;
	public bool InteractionExitFlag = false;

	private bool _interactionGate = false;
	private bool _interactionFinishedGate = false;
	private bool _physicsInteractionGate = false;
	private Node _savedUser = null;	

	public override void _Ready()
	{
		var parent = GetParent();
		parent.AddToGroup("Interactable");
	}

	public override void _Process(double delta)
	{	
		if (InteractionExitFlag)
		{
			InteractionExitFlag = false;
			EmitSignal(SignalName.InteractionFinished, _savedUser);
			_savedUser = null;
			_interactionGate = false;
			_interactionFinishedGate = false;
			_physicsInteractionGate = false;
		}

		if (_interactionFinishedGate)
		{
			_interactionFinishedGate = false;
			EmitSignal(SignalName.Interacting, _savedUser);
		}
		else
		{
			if (_interactionGate)
			{
				_interactionGate = false;
				EmitSignal(SignalName.InteractionFinished, _savedUser);
				_savedUser = null;
			}
		}

	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (_physicsInteractionGate)
		{
			_physicsInteractionGate = false;
			EmitSignal(SignalName.PhysicsInteracting, _savedUser);
		}
	}

	public void Interact(Node user)
	{
		_interactionFinishedGate = true;
		_physicsInteractionGate = true;
		if (!_interactionGate)
		{
			_interactionGate = true;
			_savedUser = user;
			EmitSignal(SignalName.InteractionStarted, user);
		}
	}

    public void Focus(Node user)
    {
		EmitSignal(SignalName.Focused, user);
    }

    public void FocusLost(Node user)
    {
		EmitSignal(SignalName.Unfocused, user);
    }
}