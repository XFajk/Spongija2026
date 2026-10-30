using Godot;
using System;

[GlobalClass]
public partial class Artefact : Resource
{
	[Export]
	public string Name { get; set; } = "";

	[Export]
	public ArtefactBodyPart BodyPart { get; set; } = ArtefactBodyPart.body;
	

	[Export]
	public Texture2D? Icon { get; set; }
}

public enum ArtefactBodyPart
{
	head,
	body
}
