using Godot;
using System;

public partial class SwapUnitSlot : Button
{
	[Export] public bool keep;
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
		if (inventory?.unitSwapping == null)
		{
			inventory?.StartSwapping(index, keep);
			Select();
		}
		else if (inventory.swappingMyUnit == keep)
		{
			inventory?.StartSwapping(index, keep);
			Select();
		}
		else
		{
			inventory?.Swap(index);
		}
	}
}
