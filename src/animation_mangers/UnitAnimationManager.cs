using Godot;
using System;

// The flags a unit model's AnimationTree reads in its transitions' advance expressions (the tree's
// advance_expression_base_node is this node). Set them through UnitNode, which plays the animations:
// IsRunning while it walks, and the one-shot flags (PlayMeleeAttack, PlayRangedAttack, PlaySpellCast) until
// their animation has started, so they must lead to a non-looping state that goes back to idle at its end.
// The one-shot animations call EmitHit from a Call Method track at the frame the attack connects, the arrow
// leaves or the spell is released; the abilities wait for it (UnitAnimation.Hit).
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
}
