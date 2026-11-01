using Godot;
using System;
using System.Threading.Tasks;

// A one-shot animation that has been started. Hit: true at the animation's hit frame (UnitAnimationManager.EmitHit),
// or at its end if it has no hit event; false right away if the model has no such animation, so the ability
// can do its own motion instead. Finished: when the animation has ended (or right away when Hit is false).
public readonly record struct UnitAnimation(Task<bool> Hit, Task Finished)
{
	// For a model without animations
	public static UnitAnimation None => new(Task.FromResult(false), Task.CompletedTask);
}

// The flags a model's AnimationTree reads in its transitions' advance expressions (the tree's
// advance_expression_base_node is this node), and the API that plays them: IsRunning while it walks (set by
// UnitNode), and the one-shot flags (PlayMeleeAttack, PlayRangedAttack, PlaySpellCast) held until their animation
// has started, so they must lead to a non-looping state that goes back to idle at its end.
// The one-shot animations call EmitHit from a Call Method track at the frame the attack connects, the arrow
// leaves or the spell is released; the abilities wait for it (UnitAnimation.Hit).
// The root of a unit's model (under its UnitNode) and of a hero's model (Arena.PlayerHeroModel).
public partial class UnitAnimationManager : Node3D
{
	[Signal]
	public delegate void HitEventHandler();

	// Called by the animations' Call Method tracks
	public void EmitHit() => EmitSignal(SignalName.Hit);

	[Export]
	public bool IsRunning;

	[Export]
	public bool MeleeAttack;

	[Export]
	public bool RangedAttack;

	[Export]
	public bool SpellCast;

	// Where the model's projectiles leave from (a hand, a bow), e.g. a Marker3D under a BoneAttachment3D so it follows
	// the animation. Used for heroes, and for units whose UnitNode has no LaunchPoint of its own.
	[Export]
	public Node3D? LaunchPoint { get; set; }

	// How long a one-shot animation (attack, cast) gets to start after its flag is set. If it doesn't start by then,
	// the model has no state for that flag, and Hit is false.
	private const double AnimationStartTimeout = 0.3;

	// Added to the animation's length as the longest wait for it to end, in case its state has no way back
	private const double AnimationEndMargin = 1.0;

	private AnimationTree? _animationTree;

	public override void _Ready()
	{
		_animationTree = this.FindDescendant<AnimationTree>();
	}

	// LaunchPoint's position, if it's set
	public Vector3? GetLaunchPosition() =>
		LaunchPoint != null && IsInstanceValid(LaunchPoint) ? LaunchPoint.GlobalPosition : null;

	// Plays the melee attack; Hit is when the weapon connects
	public UnitAnimation PlayMeleeAttack() => PlayOneShot(on => MeleeAttack = on);

	// Plays the ranged attack; Hit is when the shot leaves
	public UnitAnimation PlayRangedAttack() => PlayOneShot(on => RangedAttack = on);

	// Plays the spell cast; Hit is when the spell is released
	public UnitAnimation PlaySpellCast() => PlayOneShot(on => SpellCast = on);

	private UnitAnimation PlayOneShot(Action<bool> setFlag)
	{
		var hit = new TaskCompletionSource<bool>();
		var finished = new TaskCompletionSource();
		_ = RunOneShot(setFlag, hit, finished);
		return new UnitAnimation(hit.Task, finished.Task);
	}

	// Sets the flag until the AnimationTree's state machine enters a non-looping state (the animation it leads to),
	// then waits until the state machine leaves it again (its transition back at the end). Hit is completed by the
	// Hit signal, or at the end at the latest, so an ability never waits forever for a missing event.
	private async Task RunOneShot(Action<bool> setFlag, TaskCompletionSource<bool> hit, TaskCompletionSource finished)
	{
		void OnHit() => hit.TrySetResult(true);
		try
		{
			if (_animationTree == null
				|| _animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>() is not AnimationNodeStateMachinePlayback playback)
			{
				hit.TrySetResult(false);
				return;
			}

			Hit += OnHit;
			setFlag(true);
			StringName? state = null;
			double waited = 0;
			while (state == null && waited < AnimationStartTimeout)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (!IsInstanceValid(this))
					return;
				waited += GetProcessDeltaTime();
				if (IsOneShotState(playback.GetCurrentNode()))
					state = playback.GetCurrentNode();
			}
			setFlag(false);
			if (state == null)
			{
				hit.TrySetResult(false);
				return;
			}

			double limit = playback.GetCurrentLength() + AnimationEndMargin;
			waited = 0;
			while (playback.GetCurrentNode() == state && waited < limit)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (!IsInstanceValid(this))
					return;
				waited += GetProcessDeltaTime();
			}
		}
		finally
		{
			if (IsInstanceValid(this))
				Hit -= OnHit;
			// Whatever happened (freed node, no event, an exception), nothing awaiting these is left hanging
			hit.TrySetResult(true);
			finished.TrySetResult();
		}
	}

	// Whether the state plays an animation that doesn't loop (attacks and casts do, idle and running don't)
	private bool IsOneShotState(StringName state) =>
		_animationTree!.TreeRoot is AnimationNodeStateMachine machine
		&& machine.HasNode(state)
		&& machine.GetNode(state) is AnimationNodeAnimation node
		&& _animationTree.HasAnimation(node.Animation)
		&& _animationTree.GetAnimation(node.Animation).LoopMode == Animation.LoopModeEnum.None;
}
