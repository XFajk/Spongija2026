using Godot;
using System;

public partial class EndFightScreen : Panel
{
	public Godot.Collections.Array<StackResource> earnedUnits = new Godot.Collections.Array<StackResource>();
	public Godot.Collections.Array<StackResource> lostUnits = new Godot.Collections.Array<StackResource>();

	[Export] public PackedScene? iconPrefab;
	[Export] public Control? earnedContainer;
	[Export] public Control? lostContainer;

	public void Setup()
	{
		foreach(TextureRect _icon in earnedContainer!.GetChildren())
		{
			_icon.QueueFree();
		}

		foreach(TextureRect _icon in lostContainer!.GetChildren())
		{
			_icon.QueueFree();
		}

		for (int i = 0; i < earnedUnits.Count; i++)
		{
			TextureRect _prefab = iconPrefab!.Instantiate<TextureRect>();
			_prefab.GetNode<TextureRect>("Icon").Texture = earnedUnits[i].Unit!.texture;
			_prefab.GetNode<Label>("Amount").Text = earnedUnits[i].Quantity.ToString();
		}

		for (int i = 0; i < lostUnits.Count; i++)
		{
			TextureRect _prefab = iconPrefab!.Instantiate<TextureRect>();
			_prefab.GetNode<TextureRect>("Icon").Texture = lostUnits[i].Unit!.texture;
			_prefab.GetNode<Label>("Amount").Text = lostUnits[i].Quantity.ToString();
		}
	}
}
