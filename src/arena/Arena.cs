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
        Node3D smallTileBoard = new Node3D()
        {
            Name = "SmallTileBoard"
        };

        AddChild(smallTileBoard);

        Node3D doubleTileBoard = new Node3D()
        {
            Name = "DoubleTileBoard"
        };
        AddChild(doubleTileBoard);


        // Instantiate small tiles and arrange them in a grid within the smallTileBoard
        for (int x = TileWidthOffset; x < AreaWidth+TileWidthOffset; x++)
        {
            Node3D row = new Node3D()
            {
                Name = $"Row{x}"
            };
            smallTileBoard.AddChild(row);
            for (int y = TileHeightOffset; y < AreaHeight+TileHeightOffset; y++)
            {
                var tileInstance = SmallTile.Instantiate() as Node3D;
                tileInstance.Position = new Vector3(x/2.0f+0.25f, 0, y/2.0f+0.25f);
                tileInstance.RotationDegrees = new Vector3(-90, 0, 0);
                tileInstance.Hide();
                row.AddChild(tileInstance);
            }
        }

        // Instantiate double tiles and arrange them in a grid within the doubleTileBoard
        for (int x = TileWidthOffset; x < AreaWidth+TileWidthOffset; x++)
        {
            Node3D row = new Node3D()
            {
                Name = $"Row{x}"
            };
            doubleTileBoard.AddChild(row);
            for (int y = TileHeightOffset; y < AreaHeight+TileHeightOffset; y++)
            {
                var tileInstance = DoubleTile.Instantiate() as Node3D;
                tileInstance.Position = new Vector3(x/2.0f+0.25f, 0, y/2.0f+0.25f);
                tileInstance.RotationDegrees = new Vector3(-90, 0, 0);
                tileInstance.Hide();
                row.AddChild(tileInstance);
            }
        }
    }
}
