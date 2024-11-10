using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DotTiled;
using System;
using Color = Microsoft.Xna.Framework.Color;
using System.Collections.Generic;

namespace NuTiled.CustomTypes {
	public enum Direction {
		Up, Down, Left, Right
	}

	public class FilledShape {
		public DotTiled.Color FillColor { get; set; }
		public DotTiled.Color BorderColor { get; set; }
	}
}

//putting this here instead of in a seperate file for clarity.
//CustomTypes.FilledShape could include all of the following, but I wanted to include it as a skeleton class for creating a
//game object which doesn't have the limitations of Dottiled.CustomClassDefinition and isn't *as* tied to the Dottiled API.
//In practice, this is a bit difficult when I've set up the Draw calls to only look at DotTiled objects.
//So in the TiledMap, there is a dictionary linking each FilledShape to a DotTiled.Object, and it's positional data comes from this.
//Other parameters from the DotTiled.Object could be made into properties either in the constructor or as getters.
namespace NuTiled.GameClasses {
	public class FilledShape {
		public Color FillColor { get; set; }
		public Color BorderColor { get; set; }
		public FilledShape(CustomTypes.FilledShape shape) {
			FillColor = shape.FillColor is null ? Color.Transparent : TiledMap.ColorToColor(shape.FillColor);
			BorderColor = shape.BorderColor is null ? Color.Transparent : TiledMap.ColorToColor(shape.BorderColor);
		}

		public float PointObjectRadius = 3;

		public void Draw(SpriteBatch spriteBatch, DotTiled.Object obj, Point view_offset) {

			Vector2 position = new Vector2(obj.X, obj.Y) + view_offset.ToVector2();

			switch (obj) {
				case PolygonObject polygon: {
					var points = polygon.Points;
					if (BorderColor != Color.Transparent) {
						Primitives2D.DrawPolygon(spriteBatch, position, points, true, BorderColor);
					}
					
					break;
				}
				case EllipseObject ellipse: {
					Rectangle bounds = TiledMap.GetObjectBounds(obj);
					bounds.Offset(view_offset);
					if (FillColor != Color.Transparent) {
						Primitives2D.FillEllipse(spriteBatch, bounds, FillColor);
					}
					if (BorderColor != Color.Transparent) {
						Primitives2D.DrawEllipse(spriteBatch, bounds, BorderColor);
					}
					break;
				}
				case PointObject point: {
					Primitives2D.FillCircle(spriteBatch, position, PointObjectRadius, FillColor);
					Color borderColor = BorderColor == Color.Transparent ? FillColor : BorderColor;
					Primitives2D.DrawCircleBresenham(spriteBatch, position, PointObjectRadius, borderColor);
					break;
				}
				case PolylineObject:
					break;
				case RectangleObject: {
					Rectangle bounds = TiledMap.GetObjectBounds(obj);
					bounds.Offset(view_offset);
					Primitives2D.FillRectangle(spriteBatch, bounds, FillColor);
					Primitives2D.DrawRectangle(spriteBatch, bounds, BorderColor);

					break;
				}
			}
		}
	}
}

