using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ArenaUserInterface : Control
{
	// A spell was picked in the spellbook: one of the current stack's own spells, or a hero spell.
	[Signal]
	public delegate void SpellPickedEventHandler(SpellAbility spell, bool isHeroSpell);

	public Arena? arena;	
	public ArenaCamera? arenaCamera;

	private Control? _setupInterface;
	private Control? _battleInterface;

	// UI elements for the setup  interfaces
	private TextureButton? _startBattleButton;

	// UI elements for the battle interface
	private TextureButton? _useUnitSpellButton;
	private TextureButton? _waitButton;

	private TextureButton? _useHeroSpellButton;

	// Shown while the target of a picked spell is being chosen
	private Button? _cancelSpellButton;

	// UI elements for the spellbook
	private Control? _bookInterface;
	private AnimatedSprite2D? _spellBook;
	private Control? _spellButtonsRoot;
	private Button? _nextPageButton;
	private Button? _backPageButton;
	// The caster's mana: a bar (animated by src/shaders/mana_bar.gdshader) with the numbers on it
	private ProgressBar? _manaBar;
	private Label? _manaLabel;

	// In reading order: the left page top to bottom, then the right page. One page spread shows this many spells.
	private readonly List<SpellButton> _spellButtons = [];

	// What the open book shows: the spells, which of them can be cast now (the others are shown disabled),
	// whether they're hero spells, and the page spread (12 spells per spread).
	private List<SpellAbility> _bookSpells = [];
	private Func<SpellAbility, bool> _canCastBookSpell = _ => false;
	private bool _bookShowsHeroSpells;
	private int _bookPage;
	// The caster's mana, for the spells' tooltips
	private uint _bookMana;

	// While the book opens or flips a page, closing it and turning pages are ignored
	private bool _bookAnimating;

	private const string OpenAnimation = "open";
	private const string FlipPageAnimation = "flip_page";

	public const string MeleeCursorTexture = "uid://c21n6vo2a3nt4";

	// Scale of the sword that replaces the mouse cursor while picking where to melee from (the image is 256x256).
	[Export]
	public float MeleeCursorScale { get; set; } = 0.35f;

	private Sprite2D? _meleeCursor;

	public const string RangedCursorTexture = "uid://nidiayrbf26b";

	// Scale of the arrow that replaces the mouse cursor over an enemy that can be shot (the image is 256x256).
	[Export]
	public float RangedCursorScale { get; set; } = 0.35f;

	// The pixel of the arrow image that sits on the mouse position: the arrowhead's tip.
	[Export]
	public Vector2 RangedCursorHotspot { get; set; } = new(168, 172);

	private Sprite2D? _rangedCursor;

	// Size in pixels of the longer side of the spell's icon when it replaces the mouse cursor over a spell target
	[Export]
	public float SpellCursorSize { get; set; } = 128f;

	private Sprite2D? _spellCursor;

	// Opacity of a disabled texture button (SetButtonDisabled): low enough to read as off, not invisible
	[Export(PropertyHint.Range, "0,1")]
	public float DisabledButtonAlpha { get; set; } = 0.25f;

	public const string StackLabelScene = "uid://bcefw8pb8y3se";

	// Holds the stacks' labels, behind the rest of the interface; hidden while the spellbook is open
	private Control? _stackLabelsRoot;
	private readonly Dictionary<Stack, StackLabel> _stackLabels = [];

	public const string StackTooltipScene = "uid://dstktltp3m7na";

	// Seconds the mouse has to rest on a stack before its tooltip shows
	[Export]
	public float StackTooltipDelay { get; set; } = 0.5f;

	// From the mouse to the tooltip's top-left corner, in pixels; it's moved to stay on the screen
	[Export]
	public Vector2 StackTooltipOffset { get; set; } = new(24, 24);

	private StackTooltip? _stackTooltip;
	private Stack? _hoveredStack;
	private double _hoverTime;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// First, so a broken node path below can't keep the cursor from existing
		_meleeCursor = new Sprite2D
		{
			Name = "MeleeCursor",
			Texture = GD.Load<Texture2D>(MeleeCursorTexture),
			Scale = Vector2.One * MeleeCursorScale,
			Visible = false,
		};
		AddChild(_meleeCursor);

		_rangedCursor = new Sprite2D
		{
			Name = "RangedCursor",
			Texture = GD.Load<Texture2D>(RangedCursorTexture),
			Scale = Vector2.One * RangedCursorScale,
			Centered = false,
			Offset = -RangedCursorHotspot,
			Visible = false,
		};
		AddChild(_rangedCursor);

		// Its texture is the picked spell's icon, set when it's shown
		_spellCursor = new Sprite2D
		{
			Name = "SpellCursor",
			Visible = false,
		};
		AddChild(_spellCursor);

		// First child, so it draws behind the interfaces and the cursors
		_stackLabelsRoot = new Control
		{
			Name = "StackLabels",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_stackLabelsRoot);
		MoveChild(_stackLabelsRoot, 0);

		_setupInterface = GetNode<Control>("SetupInterface");
		_battleInterface = GetNode<Control>("BattleInterface");

		_startBattleButton = GetNode<TextureButton>("SetupInterface/StartBattle");
		_startBattleButton!.Pressed += OnStartBattleButtonPressed;

		_useUnitSpellButton = GetNode<TextureButton>("BattleInterface/BattleActions/UnitActions/UseSpell");
		_useUnitSpellButton!.Pressed += OnUseUnitSpellButtonPressed;

		_waitButton = GetNode<TextureButton>("BattleInterface/BattleActions/UnitActions/Wait");
		_waitButton!.Pressed += OnWaitButtonPressed;

		_useHeroSpellButton = GetNode<TextureButton>("BattleInterface/BattleActions/UseHeroSpell");
		_useHeroSpellButton!.Pressed += OnUseHeroSpellButtonPressed;

		_cancelSpellButton = GetNode<Button>("BattleInterface/CancelSpell");
		_cancelSpellButton.Pressed += OnCancelSpellButtonPressed;

		_bookInterface = GetNode<Control>("BookInterface");
		// Clicks on the book (also between buttons and while it animates) mustn't reach the battle behind it
		_bookInterface.MouseFilter = MouseFilterEnum.Stop;
		_spellBook = GetNode<AnimatedSprite2D>("BookInterface/SpellBook");
		_spellButtonsRoot = GetNode<Control>("BookInterface/SpellButtons");
		_nextPageButton = GetNode<Button>("BookInterface/Next");
		_nextPageButton.Pressed += OnNextPageButtonPressed;
		_backPageButton = GetNode<Button>("BookInterface/Back");
		_backPageButton.Pressed += OnBackPageButtonPressed;
		_manaBar = GetNode<ProgressBar>("BookInterface/ProgressBar");
		_manaLabel = GetNode<Label>("BookInterface/ProgressBar/ManaLable");

		_stackTooltip = GD.Load<PackedScene>(StackTooltipScene).Instantiate() as StackTooltip;
		if (_stackTooltip != null)
		{
			_stackTooltip.Visible = false;
			AddChild(_stackTooltip);
		}
		else
			GD.PushError("The stack tooltip scene's root isn't a StackTooltip.");

		SetDrawOrder();

		CollectSpellButtons(_spellButtonsRoot);
		foreach (SpellButton spellButton in _spellButtons)
			spellButton.SpellPicked += OnSpellButtonPicked;

		// Until Arena enables them at the start of a player turn
		UpdateSpellButtons(null);
		SetWaitEnabled(false);

		if (arena is not Arena a)
		{
			return;
		}

		ShowSetupInterface();
		a.SetupPlacementPhaseCompleted += ShowBattleInterface;
	}

	// Bottom to top: stack labels (0), the interfaces, the stack tooltip, then the cursors
	private void SetDrawOrder()
	{
		const int interfaces = 1;
		_setupInterface!.ZIndex = interfaces;
		_battleInterface!.ZIndex = interfaces;
		_bookInterface!.ZIndex = interfaces;
		if (_stackTooltip != null)
			_stackTooltip.ZIndex = interfaces + 1;
		_meleeCursor!.ZIndex = interfaces + 2;
		_rangedCursor!.ZIndex = interfaces + 2;
		_spellCursor!.ZIndex = interfaces + 2;
	}

	public override void _Process(double delta)
	{
		// The arrow and the spell icon follow the mouse, unlike the sword, which Arena places on the picked tile
		if (_rangedCursor != null && _rangedCursor.Visible)
			_rangedCursor.GlobalPosition = GetGlobalMousePosition();
		if (_spellCursor != null && _spellCursor.Visible)
			_spellCursor.GlobalPosition = GetGlobalMousePosition();

		UpdateStackLabels();
		UpdateStackTooltip(delta);
	}

	public override void _ExitTree()
	{
		// Never leave the game with the mouse hidden
		HideMeleeCursor();
		HideRangedCursor();
		HideSpellCursor();
	}

	// Replaces the mouse cursor with the sword: the mouse is hidden where it is (it still moves and hovers),
	// and the sword is drawn at the canvas position, rotated clockwise by the angle from pointing up.
	public void ShowMeleeCursor(Vector2 position, float rotation)
	{
		if (_meleeCursor == null)
			return;
		_meleeCursor.GlobalPosition = position;
		_meleeCursor.Rotation = rotation;
		_meleeCursor.Visible = true;
		UpdateMouseMode();
	}

	public void HideMeleeCursor()
	{
		if (_meleeCursor == null || !_meleeCursor.Visible)
			return;
		_meleeCursor.Visible = false;
		UpdateMouseMode();
	}

	// Replaces the mouse cursor with the arrow, its tip on the mouse position.
	public void ShowRangedCursor()
	{
		if (_rangedCursor == null)
			return;
		_rangedCursor.GlobalPosition = GetGlobalMousePosition();
		_rangedCursor.Visible = true;
		UpdateMouseMode();
	}

	public void HideRangedCursor()
	{
		if (_rangedCursor == null || !_rangedCursor.Visible)
			return;
		_rangedCursor.Visible = false;
		UpdateMouseMode();
	}

	// Replaces the mouse cursor with the spell's icon, centered on the mouse position and SpellCursorSize big.
	// Without an icon the normal cursor stays.
	public void ShowSpellCursor(Texture2D? icon)
	{
		if (_spellCursor == null)
			return;
		if (icon == null)
		{
			HideSpellCursor();
			return;
		}
		if (_spellCursor.Texture != icon)
		{
			_spellCursor.Texture = icon;
			Vector2 size = icon.GetSize();
			_spellCursor.Scale = Vector2.One * SpellCursorSize / Mathf.Max(1f, Mathf.Max(size.X, size.Y));
		}
		_spellCursor.GlobalPosition = GetGlobalMousePosition();
		_spellCursor.Visible = true;
		UpdateMouseMode();
	}

	public void HideSpellCursor()
	{
		if (_spellCursor == null || !_spellCursor.Visible)
			return;
		_spellCursor.Visible = false;
		UpdateMouseMode();
	}

	// The mouse is hidden while any of the cursors stands in for it
	private void UpdateMouseMode()
	{
		bool replaced = (_meleeCursor?.Visible ?? false) || (_rangedCursor?.Visible ?? false)
			|| (_spellCursor?.Visible ?? false);
		Input.MouseMode = replaced ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
	}

	private void ShowSetupInterface()
	{
		_setupInterface!.Visible = true;
		_battleInterface!.Visible = false;
	}

	private void ShowBattleInterface()
	{
		_setupInterface!.Visible = false;
		_battleInterface!.Visible = true;
	}


	// Enables Use Spell if the stack has spells of its own and Use Hero's Spell if its hero knows any (the ones the
	// book would show) and hasn't cast this round. Null, between player turns, disables both.
	public void UpdateSpellButtons(Stack? stack)
	{
		if (_useUnitSpellButton == null || _useHeroSpellButton == null)
			return;
		SetButtonDisabled(_useUnitSpellButton, stack == null || stack.Unit.Spells.Count == 0);
		SetButtonDisabled(_useHeroSpellButton, stack?.Hero is not Hero hero || hero.HasCastThisRound
			|| hero.GetKnownSpells().Count == 0);
	}

	// Disables or enables a texture button, fading a disabled one (label included) to DisabledButtonAlpha,
	// so it's obviously off but still visible. Use this instead of setting Disabled on them directly.
	private void SetButtonDisabled(TextureButton button, bool disabled)
	{
		button.Disabled = disabled;
		button.Modulate = new Color(1, 1, 1, disabled ? DisabledButtonAlpha : 1);
	}

	public void SetCancelSpellVisible(bool visible)
	{
		if (_cancelSpellButton != null)
			_cancelSpellButton.Visible = visible;
	}

	private void OnCancelSpellButtonPressed()
	{
		arena?.CancelSpellTargeting();
	}

	// The current stack's own spells; the ones it lacks the mana for are disabled
	private void OnUseUnitSpellButtonPressed()
	{
		if (arena?.IsPlayerTurn != true || arena.CurrentStack is not Stack stack)
			return;
		OpenSpellBook(stack.Unit.Spells.ToList(), spell => stack.Mana >= spell.ManaCost, heroSpells: false,
			stack.Mana, stack.Unit.MaxMana);
	}

	private void OnWaitButtonPressed()
	{
		arena?.TryWait();
	}

	// Enabled on a player turn whose stack hasn't waited this round yet
	public void SetWaitEnabled(bool enabled)
	{
		if (_waitButton != null)
			SetButtonDisabled(_waitButton, !enabled);
	}

	// The hero's spells unlocked by Knowledge; the ones it can't cast now (mana, already cast this round) are disabled
	private void OnUseHeroSpellButtonPressed()
	{
		if (arena?.IsPlayerTurn != true || arena.CurrentStack?.Hero is not Hero hero)
			return;
		OpenSpellBook(hero.GetKnownSpells(), hero.CanCast, heroSpells: true, hero.Mana, hero.MaxMana);
	}

	#region Stack labels

	// Called by Arena when it spawns the stack's unit. The side picks the tag's image.
	public void AddStackLabel(Stack stack, bool isPlayerStack)
	{
		if (_stackLabelsRoot == null || _stackLabels.ContainsKey(stack))
			return;
		if (GD.Load<PackedScene>(StackLabelScene).Instantiate() is not StackLabel label)
		{
			GD.PushError("The stack label scene's root isn't a StackLabel.");
			return;
		}
		_stackLabelsRoot.AddChild(label);
		label.Setup(stack, isPlayerStack);
		_stackLabels[stack] = label;
		PlaceStackLabel(label);
	}

	private Stack? _previewedStack;

	// Called by Arena every frame: what the hovered attack or spell would do to the stack (null for none), shown
	// under its tag. The stack previewed before is cleared.
	public void SetActionPreview(Stack? stack, int unitsKilled, int unitsRevived)
	{
		if (_previewedStack != stack && _previewedStack != null
			&& _stackLabels.TryGetValue(_previewedStack, out StackLabel? previous))
			previous.ShowPreview(0, 0);
		_previewedStack = stack;
		if (stack != null && _stackLabels.TryGetValue(stack, out StackLabel? label))
			label.ShowPreview(unitsKilled, unitsRevived);
	}

	// Called by Arena when the stack dies
	public void RemoveStackLabel(Stack stack)
	{
		if (_stackLabels.Remove(stack, out StackLabel? label))
			label.QueueFree();
	}

	// Every frame, so the labels follow moving units and the camera, and show the current number of units
	private void UpdateStackLabels()
	{
		foreach (StackLabel label in _stackLabels.Values)
		{
			label.Refresh();
			PlaceStackLabel(label);
		}
	}

	// Under the stack's feet on the screen; hidden while the stack is behind the camera
	private void PlaceStackLabel(StackLabel label)
	{
		if (arena != null && label.Stack != null && arena.GetStackLabelCanvasPoint(label.Stack) is Vector2 canvasPoint)
		{
			label.PlaceAt(canvasPoint);
			label.Visible = true;
		}
		else
			label.Visible = false;
	}

	#endregion

	#region Stack tooltip

	// Called by Arena every frame with the stack under the mouse (its unit or its tile), or null
	public void SetHoveredStack(Stack? stack)
	{
		if (stack == _hoveredStack)
			return;
		_hoveredStack = stack;
		_hoverTime = 0;
		if (_stackTooltip != null)
			_stackTooltip.Visible = false;
	}

	// Shows the tooltip once the mouse has rested on the stack for StackTooltipDelay, then keeps it current and next
	// to the mouse. Not while the spellbook is open.
	private void UpdateStackTooltip(double delta)
	{
		if (_stackTooltip == null)
			return;
		if (_hoveredStack == null || _hoveredStack.IsDead || (_bookInterface?.Visible ?? false))
		{
			_stackTooltip.Visible = false;
			return;
		}

		_hoverTime += delta;
		if (_hoverTime < StackTooltipDelay)
			return;

		if (!_stackTooltip.Visible || _stackTooltip.Stack != _hoveredStack)
			_stackTooltip.ShowStack(_hoveredStack);
		else
			_stackTooltip.Refresh();
		_stackTooltip.Visible = true;
		// Fits its content, which changes with the stack
		_stackTooltip.ResetSize();
		_stackTooltip.GlobalPosition = KeepOnScreen(GetGlobalMousePosition(), StackTooltipOffset, _stackTooltip.Size);
	}

	// Top-left corner for a popup of the size at the mouse plus the offset, flipped to the other side of the mouse
	// where it would leave the screen
	private Vector2 KeepOnScreen(Vector2 mouse, Vector2 offset, Vector2 size)
	{
		Rect2 screen = GetViewportRect();
		Vector2 position = mouse + offset;
		if (position.X + size.X > screen.End.X)
			position.X = mouse.X - offset.X - size.X;
		if (position.Y + size.Y > screen.End.Y)
			position.Y = mouse.Y - offset.Y - size.Y;
		return position.Clamp(screen.Position, (screen.End - size).Max(screen.Position));
	}

	#endregion

	#region Spellbook

	// Depth first, so the order is the reading order of the pages
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

	// Swaps the battle interface for the book, fills the first spread and plays the opening; the spells and
	// page buttons show when it's open. The mana is the caster's: the hero's, or the stack's for its own spells.
	private async void OpenSpellBook(List<SpellAbility> spells, Func<SpellAbility, bool> canCast, bool heroSpells,
		uint mana, uint maxMana)
	{
		_bookSpells = spells;
		_canCastBookSpell = canCast;
		_bookShowsHeroSpells = heroSpells;
		_bookPage = 0;
		_bookMana = mana;
		// Mana can't change while the book is open, so it's set once
		_manaLabel!.Text = $"Mana: {mana}/{maxMana}";
		// At least 1, so a caster with no mana pool shows an empty bar instead of a full one
		_manaBar!.MaxValue = Math.Max(1, maxMana);
		_manaBar.Value = mana;

		_battleInterface!.Visible = false;
		_stackLabelsRoot!.Visible = false;
		_bookInterface!.Visible = true;
		FillSpellButtons();
		await PlayBookAnimation(OpenAnimation, backwards: false);
	}

	private void CloseSpellBook()
	{
		_bookInterface!.Visible = false;
		_battleInterface!.Visible = true;
		_stackLabelsRoot!.Visible = true;
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

	// Gives the buttons the current spread's spells; buttons past the last spell are hidden
	private void FillSpellButtons()
	{
		int first = _bookPage * _spellButtons.Count;
		for (int i = 0; i < _spellButtons.Count; i++)
		{
			SpellButton button = _spellButtons[i];
			SpellAbility? spell = first + i < _bookSpells.Count ? _bookSpells[first + i] : null;
			button.Spell = spell;
			button.Visible = spell != null;
			button.Disabled = spell == null || !_canCastBookSpell(spell);
			// A disabled button keeps the normal icon even when hovered
			button.UpdateIcon();
			button.IsHeroSpell = _bookShowsHeroSpells;
			button.CantCastReason = spell != null ? GetCantCastReason(spell) : null;
		}
	}

	// For the spell's tooltip. Null if it can be cast now.
	private string? GetCantCastReason(SpellAbility spell)
	{
		if (_canCastBookSpell(spell))
			return null;
		if (_bookMana < spell.ManaCost)
			return "Not enough mana";
		if (_bookShowsHeroSpells && arena?.CurrentStack?.Hero?.HasCastThisRound == true)
			return "The hero has already cast a spell this round";
		return "Can't be cast now";
	}

	// Hides the spells and page buttons while the animation plays (from frame 0, or from the last frame backwards),
	// then shows the spells, Next if there are more spells after this spread and Back if this isn't the first.
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
		_nextPageButton.Visible = (_bookPage + 1) * _spellButtons.Count < _bookSpells.Count;
		_backPageButton.Visible = _bookPage > 0;
	}

	private void OnSpellButtonPicked(SpellAbility spell)
	{
		if (_bookAnimating)
			return;
		CloseSpellBook();
		EmitSignal(SignalName.SpellPicked, spell, _bookShowsHeroSpells);
	}

	// A click anywhere off the book closes it, and doesn't reach the battle behind it.
	// In _Input, before the GUI and the battle view get the click.
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
		arenaCamera?.IgnoreCurrentClick();
		if (!_bookAnimating)
			CloseSpellBook();
	}

	#endregion

	private void OnStartBattleButtonPressed()
	{
		arena?.EmitSignal(Arena.SignalName.SetupPlacementPhaseCompleted);
	}
}
