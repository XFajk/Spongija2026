using Godot;
using System;
using System.Threading.Tasks;

public partial class OutroScene : Control
{
	[Export] public FadeText? _text;

	public override async void _Ready()
	{
		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
		await _text!.Setup("At last, you have led the Quendies safely to the River Anduin.");
		await _text!.Setup("Your task is complete, and");
		await _text.Setup("their journey westward may now continue without you.");
		await _text.Setup("Some chOOse to remain behind, while others");
		await _text.Setup("prepare to cross the river and");
		await _text.Setup("continue towards the distant West.");

		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);

		string path = ProjectSettings.GlobalizePath($"user://saves/slot_{CurrentSave.CurrentSaveIndex}.json");

		if (FileAccess.FileExists($"user://saves/slot_{CurrentSave.CurrentSaveIndex}.json"))
		{
			Error error = DirAccess.RemoveAbsolute(path);
		}

		Control menu = GD.Load<PackedScene>("uid://ccijx38gp0hk5").Instantiate<Control>();
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, menu);
	}
}
