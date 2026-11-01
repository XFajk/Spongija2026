using Godot;

/// <summary>
/// Blender's Copy Transforms + Damped Track on one bone: the mesh takes the transform of
/// <see cref="Bone"/>'s head, then turns the least it can so <see cref="TrackAxis"/> points
/// at the bone's tail. [Tool] so the bone dropdown and the follow work in the editor too.
/// </summary>
[Tool]
public partial class FlowTransformAndDampToConstraint : MeshInstance3D
{
	public enum Axis { X, Y, Z, NegativeX, NegativeY, NegativeZ }

	private Skeleton3D? _skeleton;
	private string _bone = "";
	private int _boneIndex = -1;
	private bool _connected;

	// Not "Skeleton": MeshInstance3D already has that (the skin's skeleton path).
	[Export]
	public Skeleton3D? TargetSkeleton
	{
		get => _skeleton;
		set
		{
			if (_skeleton == value)
				return;
			DisconnectSkeleton();
			_skeleton = value;
			ConnectSkeleton();
			UpdateBoneIndex();
			NotifyPropertyListChanged(); // refills the bone dropdown
		}
	}

	/// <summary>Picked from a dropdown of the skeleton's bones (see <see cref="_ValidateProperty"/>).</summary>
	[Export]
	public string Bone
	{
		get => _bone;
		set
		{
			_bone = value;
			UpdateBoneIndex();
		}
	}

	/// <summary>
	/// The mesh's local axis that is turned toward the tail. Blender's Damped Track default is Y, but
	/// the glTF export turns an object's Blender Y into -Z (bones keep Y), hence -Z.
	/// </summary>
	[Export] public Axis TrackAxis { get; set; } = Axis.NegativeZ;

	/// <summary>
	/// Godot doesn't import bone tails. The tail is the first child bone's head; a bone without
	/// children (a leaf) uses its own +Y (Blender's head-to-tail direction, kept by glTF) instead.
	/// </summary>
	[Export] public bool UseChildBoneAsTail { get; set; } = true;

	public override void _EnterTree() => ConnectSkeleton();

	public override void _ExitTree() => DisconnectSkeleton();

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		if (property["name"].AsStringName() != PropertyName.Bone || _skeleton == null)
			return;
		property["hint"] = (int)PropertyHint.Enum;
		property["hint_string"] = _skeleton.GetConcatenatedBoneNames();
	}

	private void ConnectSkeleton()
	{
		if (_connected || _skeleton == null || !IsInsideTree())
			return;
		_skeleton.SkeletonUpdated += Apply;
		_connected = true;
	}

	private void DisconnectSkeleton()
	{
		if (!_connected)
			return;
		if (_skeleton != null && IsInstanceValid(_skeleton))
			_skeleton.SkeletonUpdated -= Apply;
		_connected = false;
	}

	private void UpdateBoneIndex() =>
		_boneIndex = _skeleton == null || string.IsNullOrEmpty(_bone) ? -1 : _skeleton.FindBone(_bone);

	/// <summary>Runs after the skeleton has posed its bones (animations and modifiers included).</summary>
	private void Apply()
	{
		if (_skeleton == null || _boneIndex < 0 || _boneIndex >= _skeleton.GetBoneCount())
			return;

		// Copy Transforms: the bone's head.
		Transform3D head = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_boneIndex);

		// Damped Track: the shortest rotation that points TrackAxis at the tail.
		Vector3 toTail = GetTail(head) - head.Origin;
		Vector3 trackAxis = head.Basis * GetAxisVector(TrackAxis);
		if (toTail.LengthSquared() > 1e-10f && trackAxis.LengthSquared() > 1e-10f)
		{
			var turn = new Quaternion(trackAxis.Normalized(), toTail.Normalized());
			head.Basis = new Basis(turn) * head.Basis;
		}

		GlobalTransform = head;
	}

	private Vector3 GetTail(Transform3D head)
	{
		if (UseChildBoneAsTail)
		{
			int[] children = _skeleton!.GetBoneChildren(_boneIndex);
			if (children.Length > 0)
				return (_skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(children[0])).Origin;
		}
		return head.Origin + head.Basis.Y;
	}

	private static Vector3 GetAxisVector(Axis axis) => axis switch
	{
		Axis.X => Vector3.Right,
		Axis.Y => Vector3.Up,
		Axis.Z => Vector3.Back,
		Axis.NegativeX => Vector3.Left,
		Axis.NegativeY => Vector3.Down,
		_ => Vector3.Forward,
	};
}
