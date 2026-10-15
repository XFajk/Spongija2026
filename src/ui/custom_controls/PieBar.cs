using Godot;
using System;

[GlobalClass]
[Tool]
public partial class PieBar : Godot.Range 
{
	[Export(PropertyHint.Range, "6,64,1")]
	public int Slices { get; set; } = 8;

	[Export]
	public Color FillColor { get; set; } = Colors.White;	

    public override void _Draw()
	{

		Vector2 center = new Vector2(Size.X / 2, Size.Y / 2);
		bool isFull = Value >= MaxValue;
		Vector2[] polygonData = new Vector2[isFull ? Slices : Slices + 2];
		if (!isFull)
		{
			polygonData[0] = center;
		}

		float angleRange = (float)(Value / (MaxValue - MinValue) * Math.Tau);
		int pointCount = isFull ? Slices : Slices + 1;
		int offset = isFull ? 0 : 1;
		for (int i = 0; i < pointCount; i++)
		{
			float angle = angleRange * i / Slices;
			polygonData[i + offset] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * new Vector2(Size.X / 2, Size.Y / 2);
		}
		DrawPolygon(polygonData, new Color[] { FillColor });
	}

    public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized)
		{
			QueueRedraw();
		}	
	}
}
