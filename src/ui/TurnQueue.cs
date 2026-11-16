using Godot;
using System.Collections.Generic;
using System.Linq;

// The turn order at the bottom of the battle screen: the stack acting now (gold frame), the rest of this round, a
// "Round N" divider, then the next round as it would start now (Battle.PredictNextRound). One square per turn: a
// frame by side (assets/ui/round_queue_tags/), with the unit's icon, or its name if it has none, and its quantity.
// Hovering a square outlines its unit (UnitNode.IsHighlighted). The squares slide to their new places when the order
// changes; a stack that waits flies to the end of the round, shrinking on the way. Longer than MaxWidth, it pans:
// press the left button anywhere on it (gaps included) and drag, with the OS's open hand cursor over it and the
// closed hand while dragging; the mouse wheel scrolls it too. There's no scrollbar.
// Made by ArenaUserInterface inside the battle interface, refreshed by it every frame.
public partial class TurnQueue : Control
{
	public Arena? Arena { get; set; }

	// Width of the strip; a longer queue scrolls. A shorter one is centered in it.
	[Export]
	public float MaxWidth { get; set; } = 820f;

	[Export]
	public float IconSize { get; set; } = 88f;

	[Export]
	public float Gap { get; set; } = 6f;

	// Room for the "Round N" divider between the rounds
	[Export]
	public float DividerWidth { get; set; } = 96f;

	// Pixels a wheel step scrolls
	[Export]
	public float WheelScrollStep { get; set; } = 60f;

	// Seconds a square takes to slide to its new place, appear or go
	[Export]
	public float MoveDuration { get; set; } = 0.35f;

	// The squares' frames: gold for the acting stack, silver for the others, by side
	[ExportGroup("Frames")]
	// player_silver.png
	[Export]
	public Texture2D? PlayerFrame { get; set; } = GD.Load<Texture2D>("uid://kgvlehnl3p84");

	// player_gold.png
	[Export]
	public Texture2D? PlayerCurrentFrame { get; set; } = GD.Load<Texture2D>("uid://clv3b4rnj2exw");

	// enemy_silver.png
	[Export]
	public Texture2D? ComputerFrame { get; set; } = GD.Load<Texture2D>("uid://dunhhv1u8mjaf");

	// enemy_gold.png
	[Export]
	public Texture2D? ComputerCurrentFrame { get; set; } = GD.Load<Texture2D>("uid://beneeq8tgmwon");

	// Inside the frame's metal edge, as a fraction of the square's size: where the unit's icon or name goes
	[Export(PropertyHint.Range, "0,0.3")]
	public float FrameInset { get; set; } = 0.11f;
	[ExportGroup("")]

	// assets/ui_themes/fantasy.tres, for the labels' font
	private const string ThemePath = "uid://bjx58o26db5so";
	private const string WaitedIcon = "uid://clx8cx7gf8m70";

	// A stack's turn in this round (0) or the next (1)
	private readonly record struct EntryKey(Stack Stack, int Round);

	private sealed class Icon
	{
		// The frame; everything else is its child
		public required TextureRect Root;
		public required Label Quantity;
		public required TextureRect Waited;
		public Tween? Move;
		public Tween? Pulse;
		public uint? ShownQuantity;
		public bool? ShownCurrent;
	}

	private ScrollContainer? _scroll;
	private Control? _content;
	// Behind the squares, over the span they take up, so a press in a gap between them starts a pan too
	private Control? _hitArea;

	// Longer than MaxWidth, so it can be panned
	private bool _scrollable;
	// Panning: the mouse's X and the scroll when the button went down. Followed every frame, not by motion events,
	// so it keeps going if the pressed square goes away mid-drag.
	private bool _dragging;
	private float _dragStartMouseX;
	private int _dragStartScroll;
	private Label? _divider;
	private Tween? _dividerMove;
	private Texture2D? _waitedTexture;

	private readonly Dictionary<EntryKey, Icon> _icons = [];
	private List<EntryKey> _shownKeys = [];
	private int _shownRound;
	private Stack? _shownCurrent;
	private Stack? _highlighted;

	public override void _Ready()
	{
		Theme = GD.Load<Theme>(ThemePath);
		_waitedTexture = GD.Load<Texture2D>(WaitedIcon);
		// The strip's empty ends let clicks through to the battle; only the span the squares take up takes the mouse
		MouseFilter = MouseFilterEnum.Ignore;

		_scroll = new ScrollContainer
		{
			Name = "Scroll",
			// Scrolls, but never shows a scrollbar: dragging and the wheel scroll it (OnStripInput)
			HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(_scroll);
		_scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		_content = new Control { Name = "Content", MouseFilter = MouseFilterEnum.Ignore };
		_scroll.AddChild(_content);

		// First child, so the squares are on top of it and still get hovered; sized by Layout
		_hitArea = new Control { Name = "HitArea", MouseFilter = MouseFilterEnum.Pass };
		_hitArea.GuiInput += OnStripInput;
		_content.AddChild(_hitArea);

		_divider = new Label
		{
			Name = "RoundDivider",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Size = new Vector2(DividerWidth, IconSize),
			LabelSettings = new LabelSettings { FontSize = 20, OutlineSize = 6, OutlineColor = Colors.Black },
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		_content.AddChild(_divider);

		// Bottom center of the battle interface
		SetAnchorsPreset(LayoutPreset.CenterBottom);
		float height = IconSize;
		OffsetLeft = -MaxWidth / 2f;
		OffsetRight = MaxWidth / 2f;
		OffsetTop = -height - 12f;
		OffsetBottom = -12f;
	}

	public override void _ExitTree()
	{
		SetHighlighted(null);
		// Never leave the closed hand as the game's cursor
		if (_dragging)
			Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
	}

	// Called every frame. Lays the squares out again only when the order changes.
	public void Refresh()
	{
		if (Arena?.Battle is not Battle battle || battle.IsOver)
		{
			Visible = false;
			SetHighlighted(null);
			EndDrag();
			return;
		}
		Visible = true;
		UpdateDrag();

		if (battle.Round != _shownRound)
			StartNewRound(battle.Round);

		Stack? current = Arena.CurrentStack;
		List<EntryKey> keys = BuildEntries(battle, current, out int thisRoundCount);
		if (!keys.SequenceEqual(_shownKeys))
		{
			Layout(keys, thisRoundCount, battle);
			_shownKeys = keys;
		}
		if (current != _shownCurrent)
		{
			// A new turn: back to the start, where the acting stack is, unless the player is panning it
			_shownCurrent = current;
			if (!_dragging)
				_scroll!.ScrollHorizontal = 0;
		}

		foreach ((EntryKey key, Icon icon) in _icons)
			UpdateIcon(key, icon, battle, current);

		// A highlighted unit that died or a square that went
		if (_highlighted != null && (_highlighted.IsDead || !_icons.Keys.Any(k => k.Stack == _highlighted)))
			SetHighlighted(null);
	}

	// This round: the acting stack, then the round queue; then the next round as predicted. The acting stack isn't
	// shown first while it's also in the queue: it has just waited (it's at the end), or the round just ended.
	private static List<EntryKey> BuildEntries(Battle battle, Stack? current, out int thisRoundCount)
	{
		var keys = new List<EntryKey>();
		if (current != null && !current.IsDead && !battle.RoundQueue.Contains(current))
			keys.Add(new EntryKey(current, 0));
		foreach (Stack stack in battle.RoundQueue.Where(s => !s.IsDead).Distinct())
			keys.Add(new EntryKey(stack, 0));
		thisRoundCount = keys.Count;
		foreach (Stack stack in battle.PredictNextRound())
			keys.Add(new EntryKey(stack, 1));
		return keys;
	}

	// The next round has begun: its squares, shown as the next round until now, become this round's
	private void StartNewRound(int round)
	{
		_shownRound = round;
		foreach (EntryKey key in _icons.Keys.Where(k => k.Round == 0).ToList())
			RemoveIcon(key);
		foreach (EntryKey key in _icons.Keys.Where(k => k.Round == 1).ToList())
		{
			_icons.Remove(key, out Icon? icon);
			_icons[key with { Round = 0 }] = icon!;
		}
		_shownKeys = [];
	}

	private void Layout(List<EntryKey> keys, int thisRoundCount, Battle battle)
	{
		bool divider = thisRoundCount < keys.Count;
		float total = keys.Count * IconSize + Mathf.Max(0, keys.Count - 1) * Gap + (divider ? DividerWidth + Gap : 0);
		// Centered while it fits, from the left once it scrolls
		float x = Mathf.Max(0, (MaxWidth - total) / 2f);
		_content!.CustomMinimumSize = new Vector2(Mathf.Max(total, MaxWidth), IconSize);
		_hitArea!.Position = new Vector2(x, 0);
		_hitArea.Size = new Vector2(total, IconSize);
		_scrollable = total > MaxWidth;
		UpdateCursorShapes();

		var keySet = keys.ToHashSet();
		foreach (EntryKey gone in _icons.Keys.Where(k => !keySet.Contains(k)).ToList())
			RemoveIcon(gone);

		for (int i = 0; i < keys.Count; i++)
		{
			if (i == thisRoundCount && divider)
			{
				PlaceDivider(new Vector2(x, 0), battle.Round + 1);
				x += DividerWidth + Gap;
			}
			Vector2 target = new(x, 0);
			if (_icons.TryGetValue(keys[i], out Icon? icon))
				MoveIcon(icon, target);
			else
				_icons[keys[i]] = AddIcon(keys[i], target);
			x += IconSize + Gap;
		}
		if (!divider)
			_divider!.Visible = false;
	}

	private void PlaceDivider(Vector2 position, int round)
	{
		_divider!.Text = $"Round {round}";
		if (!_divider.Visible)
		{
			_divider.Position = position;
			_divider.Visible = true;
			return;
		}
		_dividerMove?.Kill();
		_dividerMove = _divider.CreateTween();
		_dividerMove.TweenProperty(_divider, "position", position, MoveDuration)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	#region Squares

	private Icon AddIcon(EntryKey key, Vector2 position)
	{
		Stack stack = key.Stack;
		// The frame's texture is set by UpdateIcon, which knows whose turn it is
		var root = new TextureRect
		{
			Name = $"{stack.Unit.Name}_{key.Round}",
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			Size = new Vector2(IconSize, IconSize),
			Position = position,
			PivotOffset = new Vector2(IconSize, IconSize) / 2f,
			MouseFilter = MouseFilterEnum.Pass,
			MouseDefaultCursorShape = CurrentCursorShape,
			Modulate = new Color(1, 1, 1, 0),
		};
		_content!.AddChild(root);

		float margin = Mathf.Round(IconSize * FrameInset);
		if (stack.Unit.texture is Texture2D texture)
		{
			var picture = new TextureRect
			{
				Texture = texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			root.AddChild(picture);
			picture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, (int)margin);
		}
		else
		{
			// No icon: the unit's name in the square
			var name = new Label
			{
				Text = stack.Unit.Name,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				ClipText = true,
				LabelSettings = new LabelSettings { FontSize = 14, OutlineSize = 4, OutlineColor = Colors.Black },
				MouseFilter = MouseFilterEnum.Ignore,
			};
			root.AddChild(name);
			name.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, (int)margin);
		}

		var quantity = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			LabelSettings = new LabelSettings { FontSize = 18, OutlineSize = 6, OutlineColor = Colors.Black },
			MouseFilter = MouseFilterEnum.Ignore,
		};
		root.AddChild(quantity);
		quantity.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, (int)margin);

		// The hourglass on a stack that has waited this round
		var waited = new TextureRect
		{
			Texture = _waitedTexture,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Size = new Vector2(26, 26),
			Position = new Vector2(margin, margin),
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		root.AddChild(waited);

		root.MouseEntered += () => SetHighlighted(stack);
		root.MouseExited += () =>
		{
			if (_highlighted == stack)
				SetHighlighted(null);
		};
		root.GuiInput += OnStripInput;

		var icon = new Icon { Root = root, Quantity = quantity, Waited = waited };
		icon.Move = root.CreateTween();
		icon.Move.TweenProperty(root, "modulate:a", 1f, MoveDuration);
		return icon;
	}

	// Slides the square to its place. One moving right has jumped ahead of others (a stack that waited): it shrinks
	// on the way, so it reads as leaving its place for the end of the round.
	private void MoveIcon(Icon icon, Vector2 target)
	{
		TextureRect root = icon.Root;
		if (root.Position.IsEqualApprox(target))
			return;
		bool jump = target.X > root.Position.X + 1f;

		icon.Move?.Kill();
		icon.Move = root.CreateTween();
		icon.Move.TweenProperty(root, "position", target, MoveDuration)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		// Also finishes a fade-in the move interrupted
		icon.Move.Parallel().TweenProperty(root, "modulate:a", 1f, MoveDuration);

		if (!jump)
			return;
		icon.Pulse?.Kill();
		icon.Pulse = root.CreateTween();
		icon.Pulse.TweenProperty(root, "scale", Vector2.One * 0.6f, MoveDuration / 2f);
		icon.Pulse.TweenProperty(root, "scale", Vector2.One, MoveDuration / 2f);
	}

	private void RemoveIcon(EntryKey key)
	{
		if (!_icons.Remove(key, out Icon? icon))
			return;
		icon.Move?.Kill();
		icon.Pulse?.Kill();
		TextureRect root = icon.Root;
		root.MouseFilter = MouseFilterEnum.Ignore;
		Tween tween = root.CreateTween().SetParallel();
		tween.TweenProperty(root, "modulate:a", 0f, MoveDuration);
		tween.TweenProperty(root, "scale", Vector2.One * 0.5f, MoveDuration);
		tween.Chain().TweenCallback(Callable.From(root.QueueFree));
	}

	// Quantity, frame (gold for the acting stack, else silver, in the side's color) and the waited hourglass
	private void UpdateIcon(EntryKey key, Icon icon, Battle battle, Stack? current)
	{
		if (icon.ShownQuantity != key.Stack.Quantity)
		{
			icon.ShownQuantity = key.Stack.Quantity;
			icon.Quantity.Text = key.Stack.Quantity.ToString();
		}

		bool isCurrent = key.Round == 0 && key.Stack == current;
		if (icon.ShownCurrent != isCurrent)
		{
			icon.ShownCurrent = isCurrent;
			icon.Root.Texture = battle.IsPlayerStack(key.Stack)
				? isCurrent ? PlayerCurrentFrame : PlayerFrame
				: isCurrent ? ComputerCurrentFrame : ComputerFrame;
		}

		icon.Waited.Visible = key.Round == 0 && key.Stack.Waited;
	}

	#endregion

	#region Panning

	// The scroll container ignores the mouse, so the strip's empty ends don't block the battle; the squares and the
	// hit area behind them send their input here. A left press starts a pan, the wheel scrolls (up/down sideways
	// too, as the queue only scrolls one way). Only while it's longer than MaxWidth.
	private void OnStripInput(InputEvent @event)
	{
		if (_scroll == null || !_scrollable || @event is not InputEventMouseButton { Pressed: true } button)
			return;

		if (button.ButtonIndex == MouseButton.Left)
		{
			_dragging = true;
			_dragStartMouseX = GetGlobalMousePosition().X;
			_dragStartScroll = _scroll.ScrollHorizontal;
			// The closed hand wherever the mouse goes until the button is released, off the queue too
			Input.SetDefaultCursorShape(Input.CursorShape.Drag);
			UpdateCursorShapes();
			AcceptEvent();
			return;
		}

		int direction = button.ButtonIndex switch
		{
			MouseButton.WheelUp or MouseButton.WheelLeft => -1,
			MouseButton.WheelDown or MouseButton.WheelRight => 1,
			_ => 0,
		};
		if (direction == 0)
			return;
		_scroll.ScrollHorizontal += Mathf.RoundToInt(direction * WheelScrollStep);
		AcceptEvent();
	}

	// Every frame while panning: the content follows the mouse, until the button is released
	private void UpdateDrag()
	{
		if (!_dragging)
			return;
		if (!Input.IsMouseButtonPressed(MouseButton.Left) || !_scrollable)
		{
			EndDrag();
			return;
		}
		float moved = GetGlobalMousePosition().X - _dragStartMouseX;
		_scroll!.ScrollHorizontal = _dragStartScroll - Mathf.RoundToInt(moved);
	}

	private void EndDrag()
	{
		if (!_dragging)
			return;
		_dragging = false;
		Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
		UpdateCursorShapes();
	}

	// The OS's open hand over a queue that can be panned, the closed hand while panning, else the normal arrow
	private Control.CursorShape CurrentCursorShape =>
		_dragging ? CursorShape.Drag : _scrollable ? CursorShape.CanDrop : CursorShape.Arrow;

	private void UpdateCursorShapes()
	{
		CursorShape shape = CurrentCursorShape;
		if (_hitArea != null)
			_hitArea.MouseDefaultCursorShape = shape;
		foreach (Icon icon in _icons.Values)
			icon.Root.MouseDefaultCursorShape = shape;
	}

	#endregion

	private void SetHighlighted(Stack? stack)
	{
		if (_highlighted == stack)
			return;
		if (_highlighted?.NodeRepresentation is UnitNode previous && IsInstanceValid(previous))
			previous.IsHighlighted = false;
		_highlighted = stack;
		if (stack?.NodeRepresentation is UnitNode unit && IsInstanceValid(unit))
			unit.IsHighlighted = true;
	}
}
