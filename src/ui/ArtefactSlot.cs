using Godot;
using System;

public partial class ArtefactSlot : Button
{
	[Export] public ArtefactBodyPart part;
	[Export] Texture2D[]? smallIcons;
	[Export] Texture2D[]? largeIcons;
	[Export] TextureRect? smallIcon;
	[Export] public bool equipped;
	[Export] public int index;
	Inventory? inventory;
	[Export] public TextureRect? image;
	[Export] Control? selected;

	public override void _Ready()
	{
		inventory = GetNode<Inventory>("/root/Map/AdventureCharacter/Inventory");
		Pressed += Click;
		Deselect();
	}


	public void Setup(Texture2D texture, ArtefactBodyPart _part)
	{
		part = _part;
		image!.Texture = texture;

		smallIcon?.Show();

		smallIcon!.Texture = smallIcons![GetPartNumber()];
	}

	public void Reset()
	{
		smallIcon?.Hide();

		image!.Texture = largeIcons![GetPartNumber()];
	}

	public void Select()
	{
		selected?.Show();
	}

	public void Deselect()
	{
		selected?.Hide();
	}

	public void Click()
	{
		if (inventory?.artefactEquipping == null)
		{
			if (equipped)
			{
				if (inventory?.EquippedArtefacts?[index] != null)
				{
					inventory?.StartEquipping(index, equipped);
					Select();
				}
			}
			else
			{
				inventory?.StartEquipping(index, equipped);
				Select();
			}
		}
		else
		{
			if (inventory?.artefactEquipped != equipped && inventory?.artefactEquipping.BodyPart == part)
			{
				inventory?.Equip(index);
			}
			else
			{
				if (equipped)
				{
					if (inventory?.EquippedArtefacts?[index] != null)
					{
						inventory?.StartEquipping(index, equipped);
						Select();
					}
				}
				else
				{
					inventory?.StartEquipping(index, equipped);
					Select();
				}
			}
		}
	}

	int GetPartNumber()
	{
		switch(part)
		{
			case ArtefactBodyPart.head:
				return 0;
			case ArtefactBodyPart.body:
				return 1;
			default:
				return 0;
		}
	}
}
