using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DotTiled;
using System;
using Color = Microsoft.Xna.Framework.Color;
using System.Collections.Generic;

namespace NuTiled.CustomTypes;


public enum CardinalDirection {
	NE, SE, SW, NW
}
public enum Chirality {
	CounterClockwise,
	ClockWise
}
public enum Direction {
	Up, Down, Left, Right
}
public enum DoorType {
	Local, Master
}
public enum KeyType {
	Local, Master
}
public enum MovePattern {
	BackAndForth,
	Clockwise,
	Counterclockwise
}
public enum SpawnType {
	Demonhead,
	Sportsman,
	Ghost,
	Ricochet,
	Comet,
	Goblin,
	Salamander
}

public class FilledShape {
	public DotTiled.Color FillColor { get; set; }

	public float PointObjectRadius = 2;

	public void Draw(SpriteBatch spriteBatch, DotTiled.Object obj, Point view_offset) {
		Color fill_color = TiledMap.ColorToColor(FillColor);
		Vector2 position = new Vector2(obj.X, obj.Y) + view_offset.ToVector2();

		switch (obj) {
			case PolygonObject polygon: {
				var points = polygon.Points;
				for (int i = 0; i < points.Count; i++) {
					Vector2 p1 = points[i] + position;
					Vector2 p2 = points[(i + 1) % points.Count] + position;
					Primitives2D.DrawLine(spriteBatch, p1, p2, fill_color);
				}
				break;
			}
			case EllipseObject ellipse: {
				Rectangle bounds = TiledMap.ObjectBounds(ellipse);
				bounds.Offset(view_offset);
				Primitives2D.FillEllipse(spriteBatch, bounds, fill_color);
				break;
			}
			case PointObject point: { }
				Primitives2D.FillCircle(spriteBatch, position, PointObjectRadius, fill_color);
				break;
			case PolylineObject:
				break;
			case RectangleObject: {
				Rectangle bounds = TiledMap.ObjectBounds(obj);
				bounds.Offset(view_offset);
				Primitives2D.FillRectangle(spriteBatch, bounds, fill_color);
				break;
			}
		}
	}
}
