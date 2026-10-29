using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class SpellDisplay : Control
{
	private Inventory? inventory;
	private Control? _bookInterface;
	private AnimatedSprite2D? _spellBook;
	private Control? _spellButtonsRoot;
	private Button? _nextPageButton;
	private Button? _backPageButton;
	private ProgressBar? _manaBar;
	private Label? _manaLabel;

	private readonly List<SpellButton> _spellButtons = [];

	private List<SpellAbility>? _bookSpells;
	private int _bookPage;
	private uint _bookMana;

	private bool _bookAnimating;

	private const string OpenAnimation = "open";
	private const string FlipPageAnimation = "flip_page";

	public override void _Ready()
	{
		inventory = GetNode<Inventory>("/root/Map/AdventureCharacter/Inventory");

		_bookInterface = GetNode<Control>("BookInterface");
		_bookInterface.MouseFilter = MouseFilterEnum.Stop;
		_spellBook = GetNode<AnimatedSprite2D>("BookInterface/SpellBook");
		_spellButtonsRoot = GetNode<Control>("BookInterface/SpellButtons");
		_nextPageButton = GetNode<Button>("BookInterface/Next");
		_nextPageButton.Pressed += OnNextPageButtonPressed;
		_backPageButton = GetNode<Button>("BookInterface/Back");
		_backPageButton.Pressed += OnBackPageButtonPressed;
		_manaBar = GetNode<ProgressBar>("BookInterface/ProgressBar");
		_manaLabel = GetNode<Label>("BookInterface/ProgressBar/ManaLable");

		CollectSpellButtons(_spellButtonsRoot!);
	}

	public void OpenBook()
	{
		inventory?.character?.HidePanel();

		OpenSpellBook(inventory!.spells!, 30, 50);
	}

	private void CollectSpellButtons(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is SpellButton spellButton)
				_spellButtons.Add(spellButton);
			else
				CollectSpellButtons(child);
		}
	}

	private async void OpenSpellBook(SpellAbility[] spells, uint mana, uint maxMana)
	{
		_bookSpells = new List<SpellAbility>();
		for (int i = 0; i < spells.Length; i++)
		{
			_bookSpells.Add(spells[i]);
		}

		GD.Print(_bookSpells.Count);

		_bookPage = 0;
		_bookMana = mana;
		_manaLabel!.Text = $"Mana: {mana}/{maxMana}";
		_manaBar!.MaxValue = Math.Max(1, maxMana);
		_manaBar.Value = mana;

		_bookInterface!.Visible = true;
		FillSpellButtons();
		await PlayBookAnimation(OpenAnimation, backwards: false);
	}

	private void CloseSpellBook()
	{
		_bookInterface!.Visible = false;
	}

	private async void OnNextPageButtonPressed()
	{
		if (_bookAnimating)
			return;
		_bookPage++;
		FillSpellButtons();
		await PlayBookAnimation(FlipPageAnimation, backwards: false);
	}

	private async void OnBackPageButtonPressed()
	{
		if (_bookAnimating || _bookPage == 0)
			return;
		_bookPage--;
		FillSpellButtons();
		await PlayBookAnimation(FlipPageAnimation, backwards: true);
	}

	private void FillSpellButtons()
	{
		int first = _bookPage * _spellButtons.Count;
		for (int i = 0; i < _spellButtons.Count; i++)
		{
			SpellButton button = _spellButtons[i];
			SpellAbility? spell = first + i < _bookSpells?.Count ? _bookSpells[first + i] : null;
			button.Spell = spell;
			button.Visible = spell != null;
			button.UpdateIcon();
		}
	}

	private async Task PlayBookAnimation(string animation, bool backwards)
	{
		_bookAnimating = true;
		_spellButtonsRoot!.Visible = false;
		_nextPageButton!.Visible = false;
		_backPageButton!.Visible = false;

		// Setting the animation resets the frame to 0, so the frame is set after it
		_spellBook!.Animation = animation;
		if (backwards)
		{
			_spellBook.Frame = _spellBook.SpriteFrames.GetFrameCount(animation) - 1;
			_spellBook.PlayBackwards();
		}
		else
		{
			_spellBook.Frame = 0;
			_spellBook.Play();
		}
		await ToSignal(_spellBook, AnimatedSprite2D.SignalName.AnimationFinished);

		_bookAnimating = false;
		// Closed while animating is blocked, but the whole UI may be gone (end of the battle)
		if (!IsInsideTree() || !_bookInterface!.Visible)
			return;
		_spellButtonsRoot.Visible = true;
		_nextPageButton.Visible = (_bookPage + 1) * _spellButtons.Count < _bookSpells?.Count;
		_backPageButton.Visible = _bookPage > 0;
	}

	public override void _Input(InputEvent @event)
	{
		if (_bookInterface == null || !_bookInterface.Visible)
			return;
		if (@event is not InputEventMouseButton { Pressed: true } click)
			return;
		if (_bookInterface.GetGlobalRect().HasPoint(click.Position))
			return;

		GetViewport().SetInputAsHandled();
		// The camera polls the button instead of reading events, so it has to be told this click is taken
		if (!_bookAnimating)
			CloseSpellBook();
	}
}
