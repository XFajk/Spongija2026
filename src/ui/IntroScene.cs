using Godot;
using System;
using System.Threading.Tasks;

public partial class IntroScene : Control
{
	[Export] public FadeText? _text;

	public override async void _Ready()
	{
		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
		await _text!.Setup("The Quendies have awoken beneath the stars of Cuiviénen,");
		await _text!.Setup("and their time in these lands has come to an end.");
		await _text.Setup("A call from the West has been answered,");
		await _text.Setup("and the long road to Valinor now lies ahead.");
		await _text.Setup("The journey will be perilous, but the Elves must be guided");
		await _text.Setup("through the unknown lands of Middle-earth.");

		Node3D map = GD.Load<PackedScene>("uid://cfsvco3a36qfu").Instantiate<Node3D>();
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, map);
	}
}
