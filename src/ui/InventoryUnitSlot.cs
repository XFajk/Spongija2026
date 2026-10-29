using Godot;
using System;

public partial class InventoryUnitSlot : Button
{
	[Export] public int index;
	Inventory? inventory;
	[Export] public Label? amountText;
	[Export] public TextureRect? image;
	[Export] Control? selected;
	public int amount;

	public override void _Ready()
	{
		inventory = GetNode<Inventory>("/root/Map/AdventureCharacter/Inventory");
		Pressed += Click;
		Deselect();
	}


	public void Setup(int _amount, Texture2D texture)
	{
		amount = _amount;
		amountText?.Show();
		amountText!.Text = amount.ToString();
		image!.Texture = texture;
		image!.Show();
	}

	public void Reset()
	{
		amountText?.Hide();
		amount = 0;
		image!.Hide();
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
		if (amount == 0)
		{
			if (inventory!.unitSplitting != null)
			{
				inventory!.SetupSplit(index);
			}

			GD.Print("Empty");
		}
		else
		{
			if (inventory?.resourceCombining != "")
			{
				inventory?.Combine(index);
			}
			else
			{
				if (inventory?.units?[index].Unit == inventory?.unitSplitting)
				{
					if (inventory!.unitSplitting != null)
					{
						inventory!.SetupSplit(index);
						GD.Print("Same");
					}
				}
				else
				{
					inventory!.StartSplitting(index);
					GD.Print("Start");
				}
			}
		}
	}
}
