using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// The tutorials' UI, on the map (CanvasLayer/Canvas/PopoutContainer in map.tscn) and in battle (made by BattleTutorial
// through Create):
// - popouts (scenes/ui/popout.tscn): stacked, each closed by its button, pausing the tree until the last is closed
// - hints (scenes/ui/tutorial_hint.tscn): text at the top of the screen that pauses nothing and ignores the mouse,
//   shown while the player tries what it explains, until the caller hides it
public partial class PopoutManager : Control
{
	public const string DefaultPopoutScene = "uid://3me630kro538";
	public const string DefaultHintScene = "res://scenes/ui/tutorial_hint.tscn";

	// Emitted when the last open popout is closed
	[Signal] public delegate void AllClosedEventHandler();

	[Export] AdventureCamera? camera;
	// Both default to the scenes above when not set (as when made in code)
	[Export] PackedScene? popoutPrefab;
	[Export] PackedScene? hintPrefab;

	[ExportGroup("Title")]
	// Space kept free on each side of a title; a longer one is shrunk to fit
	[Export] float titleMargin = 24;
	[Export] int minTitleFontSize = 20;

	[ExportGroup("Animations")]
	// Loaded only while a popout shows an animation: the frames are big, so they aren't kept in memory with the map
	[Export(PropertyHint.File, "*.tres")] string animationsPath = "res://assets/tutorial_animations/tutorial_animations.tres";
	// The size of a popout with an animation; the animation sits under the title and the text under it
	[Export] Vector2 animatedPopoutSize = new(800, 820);
	[Export] float animatedTextTop = 540;

	[ExportGroup("Hints")]
	// From the top of the screen to the first hint
	[Export] float hintsTopMargin = 16;
	[Export] float hintFadeDuration = 0.25f;

	Godot.Collections.Array<Control> currentPopouts = new Godot.Collections.Array<Control>();
	// Holds the hints, one under another, at the top center; made in _Ready, before any popout, so popouts cover it
	VBoxContainer? hints;

	public bool IsOpen => currentPopouts.Count > 0;

	// A full-screen manager under the parent (a CanvasLayer), for scenes without one of their own
	public static PopoutManager Create(Node parent)
	{
		var manager = new PopoutManager { Name = "PopoutManager" };
		parent.AddChild(manager);
		manager.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		return manager;
	}

	public override void _Ready()
	{
		popoutPrefab ??= GD.Load<PackedScene>(DefaultPopoutScene);
		hintPrefab ??= GD.Load<PackedScene>(DefaultHintScene);
		MouseFilter = MouseFilterEnum.Ignore;

		hints = new VBoxContainer { Name = "Hints", MouseFilter = MouseFilterEnum.Ignore };
		hints.AddThemeConstantOverride("separation", 8);
		AddChild(hints);
		hints.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
		hints.GrowHorizontal = GrowDirection.Both;
		hints.OffsetTop = hintsTopMargin;
	}

	public override void _ExitTree()
	{
		// Never leave the tree paused behind
		if (IsOpen)
			GetTree().Paused = false;
	}

	#region Popouts

	// animations: the SpriteFrames' animation names to show, played one after another in a loop
	public void SpawnPopout(string _title, string _text, params string[] animations)
	{
		camera?.HideCustomCursor();
		// A custom cursor (the battle's sword or arrow) may have hidden the mouse
		Input.MouseMode = Input.MouseModeEnum.Visible;
		MouseFilter = MouseFilterEnum.Stop;
		Control _currentPopout = popoutPrefab!.Instantiate<Control>();
		if (animations.Length > 0)
			_currentPopout.CustomMinimumSize = animatedPopoutSize;
		AddChild(_currentPopout);
		_currentPopout!.SetAnchorsPreset(Control.LayoutPreset.Center);

		Label label = _currentPopout.GetNode<Label>("Label");
		Label text = _currentPopout.GetNode<Label>("%Text");
		Button button = _currentPopout.GetNode<Button>("Button");

		label.Text = _title;
		FitTitle(label, Mathf.Max(_currentPopout.CustomMinimumSize.X, _currentPopout.Size.X));
		text.Text = _text;

		if (animations.Length > 0)
		{
			// The text scrolls if it doesn't fit under the animation
			_currentPopout.GetNode<Control>("Scroll").SetAnchorAndOffset(Side.Top, 0, animatedTextTop);
			ShowAnimations(_currentPopout, animations);
		}

		button.Pressed += ClosePopout;

		currentPopouts!.Add(_currentPopout);

		GetTree().Paused = true;
	}

	// The title is one line: a long one gets a smaller font until it fits the popout's width (less titleMargin each
	// side), down to minTitleFontSize
	void FitTitle(Label title, float popoutWidth)
	{
		if (title.LabelSettings?.Font is not Font font)
			return;
		float available = popoutWidth - 2 * titleMargin;
		int size = title.LabelSettings.FontSize;
		int outline = title.LabelSettings.OutlineSize;
		while (size > minTitleFontSize
			&& font.GetStringSize(title.Text, HorizontalAlignment.Left, -1, size).X + outline > available)
			size--;
		if (size == title.LabelSettings.FontSize)
			return;
		// The settings are the scene's sub-resource, shared by every popout
		var settings = (LabelSettings)title.LabelSettings.Duplicate();
		settings.FontSize = size;
		title.LabelSettings = settings;
	}

	void ShowAnimations(Control popout, string[] animations)
	{
		if (GD.Load(animationsPath) is not SpriteFrames frames)
		{
			GD.PushError($"PopoutManager: {animationsPath} is not a SpriteFrames");
			return;
		}

		popout.GetNode<Control>("Animation").Visible = true;
		AnimatedSprite2D sprite = popout.GetNode<AnimatedSprite2D>("Animation/Sprite");
		sprite.SpriteFrames = frames;

		int current = 0;
		// The animations loop in the resource, so each one's end is AnimationLooped; then the next one plays
		if (animations.Length > 1)
			sprite.AnimationLooped += () =>
			{
				current = (current + 1) % animations.Length;
				sprite.Play(animations[current]);
			};
		sprite.Play(animations[0]);
	}

	public void ClosePopout()
	{
		if (currentPopouts.Count == 0)
			return;
		currentPopouts![currentPopouts.Count - 1]?.QueueFree();
		currentPopouts.RemoveAt(currentPopouts.Count - 1);

		if (currentPopouts.Count == 0)
		{
			GetTree().Paused = false;
			MouseFilter = MouseFilterEnum.Ignore;
			EmitSignal(SignalName.AllClosed);
		}
	}

	// Completes when no popout is open (at once if none is)
	public async Task WaitUntilClosed()
	{
		if (IsOpen)
			await ToSignal(this, SignalName.AllClosed);
	}

	#endregion

	#region Hints

	// Shows a hint under the ones already shown, until HideHint
	public Control ShowHint(string title, string text)
	{
		Control hint = hintPrefab!.Instantiate<Control>();
		hint.GetNode<Label>("%Title").Text = title;
		hint.GetNode<Label>("%Text").Text = text;
		hint.Modulate = new Color(1, 1, 1, 0);
		hints!.AddChild(hint);
		hint.CreateTween().TweenProperty(hint, "modulate:a", 1f, hintFadeDuration);
		return hint;
	}

	public void HideHint(Control? hint)
	{
		if (hint == null || !IsInstanceValid(hint) || hint.IsQueuedForDeletion())
			return;
		Tween tween = hint.CreateTween();
		tween.TweenProperty(hint, "modulate:a", 0f, hintFadeDuration);
		tween.TweenCallback(Callable.From(hint.QueueFree));
	}

	public void HideAllHints()
	{
		if (hints == null)
			return;
		foreach (Node hint in hints.GetChildren())
			hint.QueueFree();
	}

	#endregion
}
