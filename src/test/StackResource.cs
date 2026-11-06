using Godot;


[GlobalClass]
public partial class StackResource : Resource
{
    [Export]
    public Unit? Unit { get; set; } = null;

    [Export]
    public uint Quantity { get; set; } = 0;
}