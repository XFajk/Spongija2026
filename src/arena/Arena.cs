using Godot;
using System;

public partial class Arena : Node3D
{
	[Export]
    public PackedScene SmallTile;

    [Export]
    public PackedScene DoubleTile;

    [Export]
    public uint AreaWidth;

    [Export]
    public uint AreaHeight;

    [Export]
    public int TileWidthOffset;

    [Export]
    public int TileHeightOffset;

    public override void _Ready()
    {
        for (int x = TileWidthOffset; x < AreaWidth+TileWidthOffset; x++)
        {
            for (int y = TileHeightOffset; y < AreaHeight+TileHeightOffset; y++)
            {
                var tileInstance = SmallTile.Instantiate() as Node3D;
                tileInstance.Position = new Vector3(x/2.0f+0.25f, 0, y/2.0f+0.25f);
                tileInstance.RotationDegrees = new Vector3(-90, 0, 0);
                AddChild(tileInstance);
            }
        }
    }
}
