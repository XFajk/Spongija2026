using Godot;

/// <summary>
/// Blender's Copy Location + Copy Rotation on one bone, both in World Space, with Mix: Replace.
/// Copy Location moves the node to a point between <see cref="Bone"/>'s head and tail
/// (<see cref="HeadTail"/>) on the checked axes; Copy Rotation takes the bone's rotation on the checked
/// axes. Unchecked axes keep the node's own value: its transform when it entered the tree.
/// [Tool] so the bone dropdown and the follow work in the editor too.
///
/// Axes are Godot's world axes. glTF turns Blender's Z-up into Godot's Y-up, so in Blender terms:
/// Blender X = Godot X, Blender Y = Godot Z (flipped), Blender Z = Godot Y. A Blender setup of
/// location X and rotation Y + Z is location X and rotation Z + Y here. Flipped only matters for Invert.
/// </summary>
[Tool]
public partial class CopyLocationRotationConstraint : Node3D
{
	private Skeleton3D? _skeleton;
	private string _bone = "";
	private int _boneIndex = -1;
	private bool _connected;

	// The node's own transform (parent space), what unchecked axes and Influence below 1 fall back to
	private Transform3D _own = Transform3D.Identity;

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
	/// Godot doesn't import bone tails. The tail is the first child bone's head; a bone without
	/// children (a leaf) uses its own +Y (Blender's head-to-tail direction, kept by glTF) instead.
	/// </summary>
	[Export] public bool UseChildBoneAsTail { get; set; } = true;

	[ExportGroup("Copy Location")]
	[Export] public bool CopyLocation { get; set; } = true;

	/// <summary>Where on the bone: 0 is its head, 1 its tail.</summary>
	[Export(PropertyHint.Range, "0,1")] public float HeadTail { get; set; }

	[Export] public bool LocationX { get; set; } = true;
	[Export] public bool LocationY { get; set; } = true;
	[Export] public bool LocationZ { get; set; } = true;

	[Export] public bool InvertLocationX { get; set; }
	[Export] public bool InvertLocationY { get; set; }
	[Export] public bool InvertLocationZ { get; set; }

	/// <summary>Adds the node's own location to the copied one (on the checked axes), like Blender's Offset.</summary>
	[Export] public bool LocationOffset { get; set; }

	[Export(PropertyHint.Range, "0,1")] public float LocationInfluence { get; set; } = 1f;

	[ExportGroup("Copy Rotation")]
	[Export] public bool CopyRotation { get; set; } = true;

	/// <summary>
	/// The Euler order the rotation is split into axes with. Blender's Default (XYZ) is XZY here,
	/// because of the Z-up to Y-up swap.
	/// </summary>
	[Export] public EulerOrder RotationOrder { get; set; } = EulerOrder.Xzy;

	[Export] public bool RotationX { get; set; } = true;
	[Export] public bool RotationY { get; set; } = true;
	[Export] public bool RotationZ { get; set; } = true;

	[Export] public bool InvertRotationX { get; set; }
	[Export] public bool InvertRotationY { get; set; }
	[Export] public bool InvertRotationZ { get; set; }

	[Export(PropertyHint.Range, "0,1")] public float RotationInfluence { get; set; } = 1f;

	public override void _EnterTree()
	{
		_own = Transform;
		ConnectSkeleton();
	}

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

		// The node's own transform in world space
		Transform3D own = GetParentNode3D() is Node3D parent ? parent.GlobalTransform * _own : _own;
		Transform3D head = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_boneIndex);

		Vector3 position = own.Origin;
		if (CopyLocation)
			position = own.Origin.Lerp(GetCopiedLocation(own.Origin, head), LocationInfluence);

		Basis basis = own.Basis;
		if (CopyRotation)
		{
			Vector3 scale = own.Basis.Scale;
			Quaternion ownRotation = own.Basis.Orthonormalized().GetRotationQuaternion();
			Quaternion copied = GetCopiedRotation(own.Basis, head.Basis).GetRotationQuaternion();
			basis = new Basis(ownRotation.Slerp(copied, RotationInfluence)).Scaled(scale);
		}

		GlobalTransform = new Transform3D(basis, position);
	}

	// The point HeadTail of the way along the bone, on the checked axes; the own location on the others
	private Vector3 GetCopiedLocation(Vector3 own, Transform3D head)
	{
		Vector3 target = head.Origin.Lerp(GetTail(head), HeadTail);
		return new Vector3(
			CopyAxis(LocationX, InvertLocationX, own.X, target.X, LocationOffset),
			CopyAxis(LocationY, InvertLocationY, own.Y, target.Y, LocationOffset),
			CopyAxis(LocationZ, InvertLocationZ, own.Z, target.Z, LocationOffset));
	}

	// Both rotations split into Euler angles in RotationOrder: the bone's on the checked axes, the own on the others
	private Basis GetCopiedRotation(Basis own, Basis bone)
	{
		Vector3 ownAngles = own.Orthonormalized().GetEuler(RotationOrder);
		Vector3 boneAngles = bone.Orthonormalized().GetEuler(RotationOrder);
		var angles = new Vector3(
			CopyAxis(RotationX, InvertRotationX, ownAngles.X, boneAngles.X, offset: false),
			CopyAxis(RotationY, InvertRotationY, ownAngles.Y, boneAngles.Y, offset: false),
			CopyAxis(RotationZ, InvertRotationZ, ownAngles.Z, boneAngles.Z, offset: false));
		return Basis.FromEuler(angles, RotationOrder);
	}

	private static float CopyAxis(bool copy, bool invert, float own, float target, bool offset)
	{
		if (!copy)
			return own;
		float value = invert ? -target : target;
		return offset ? value + own : value;
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
}
