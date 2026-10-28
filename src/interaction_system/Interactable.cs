using System.Diagnostics;
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

	// While true, a running interaction keeps going (Interacting fires every frame) even when
	// Interact stops being called, e.g. the ray misses or interact is released, until the lock is lifted
	public bool InteractionLock = false;
	public bool InteractionExitFlag = false;
	// While true and this interaction is locked, focus is frozen too: nothing gains or loses focus.
	// Off by default, so e.g. tiles still react to hovering while a stack is being dragged
	public bool FocusLock = false;

	// The interactable whose locked interaction is running; users shouldn't interact with anything else meanwhile
	public static Interactable? LockedInteractable { get; private set; } = null;

	private bool IsLocked => InteractionLock && _interactionGate;

	private bool _interactionGate = false;
	private bool _interactionFinishedGate = false;
	private bool _physicsInteractionGate = false;
	private Node? _savedUser = null;	

	public override void _Ready()
	{
		var parent = GetParent();
		parent.AddToGroup("Interactable");
	}

	public override void _Process(double delta)
	{	
		// Cleared even with no interaction running, so a stale flag can't end the next one instantly
		bool exitRequested = InteractionExitFlag;
		InteractionExitFlag = false;
		if (exitRequested && _interactionGate)
		{
			Debug.Assert(_savedUser != null);
			EmitSignal(SignalName.InteractionFinished, _savedUser);
			_savedUser = null;
			_interactionGate = false;
			_interactionFinishedGate = false;
			_physicsInteractionGate = false;
		}

		if (_interactionFinishedGate || IsLocked)
		{
			_interactionFinishedGate = false;
			Debug.Assert(_savedUser != null);
			EmitSignal(SignalName.Interacting, _savedUser);
		}
		else
		{
			if (_interactionGate)
			{
				_interactionGate = false;
				// Cleared with the user: at a frame rate above the physics tick rate, a click can start and
				// finish before _PhysicsProcess runs, which would then emit without a user
				_physicsInteractionGate = false;
				Debug.Assert(_savedUser != null);
				EmitSignal(SignalName.InteractionFinished, _savedUser);
				_savedUser = null;
			}
		}

		UpdateLockedInteractable();
	}

	public override void _ExitTree()
	{
		if (LockedInteractable == this)
			LockedInteractable = null;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (_physicsInteractionGate || IsLocked)
		{
			_physicsInteractionGate = false;
			Debug.Assert(_savedUser != null);
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
		// InteractionStarted handlers may have just set the lock
		UpdateLockedInteractable();
	}

	private void UpdateLockedInteractable()
	{
		if (IsLocked)
			LockedInteractable = this;
		else if (LockedInteractable == this)
			LockedInteractable = null;
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
