using System;
using System.Collections.Generic;
using System.IO;
using DotTiled;
using DotTiled.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Color = Microsoft.Xna.Framework.Color;
using System.Linq;

namespace NuTiled;
public class TiledMap {
	//Trying to keep instance properties to a minimum and not make redundant classes that copy data.
	//Rendering is presently very "immediate mode," with no data being retained other than from the loaded Dottiled classes.
	//Planning to do a version where some converted data is retained and compare performance.

	//Most positional float values are cast to int. this step could be pushed forward to the spritebatch.draw calls
	//by using Vector2s instead of Points everywhere, for higher accuracy.
	//In this case, a RectangleF struct (like monogame extended) could be introduced.

	public Map Map { get; }
	public string MapFile { get; }
	public string ContentDirectory { get; }

	/// <summary>
	/// Only current Game1 tiledMap is reloaded.
	/// </summary>
	public static bool ReloadFlag { get; set; }
	public Dictionary<Tileset, Texture2D> TilemapTextures { get; }
	public Dictionary<Tile, Texture2D> TileCollectionTextures { get; }
	public Dictionary<ImageLayer, Texture2D> ImageLayerTextures { get; }
	public Color BackgroundColor => ColorToColor(Map.BackgroundColor);


	public static Rectangle TileSourceBounds(Tile tile) {
		return new((int)tile.X, (int)tile.Y, (int)tile.Width, (int)tile.Height);
	}
	public static Point TileSize(Tile tile) {
		return new((int)tile.Width, (int)tile.Height);
	}
	public static Point TileSize(Map map) {
		return new Point((int)map.TileWidth, (int)map.TileHeight);
	}
	public static Point TileSize(Tileset tileset) {
		return new Point((int)tileset.TileWidth, (int)tileset.TileHeight);
	}
	public static Point LayerOffset(BaseLayer layer) {
		return new Point((int)layer.OffsetX, (int)layer.OffsetY);
	}
	public static Vector2 LayerParallax(BaseLayer layer) {
		return new Vector2(layer.ParallaxX, layer.ParallaxY);
	}
	public static Rectangle ObjectBounds(DotTiled.Object obj) {
		return new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height);
	}



	public TiledMap(ContentManager content, string path, string map_filename) {
		MapFile = map_filename;
		ContentDirectory = path;

		//Using DotTiled's loader.
		//Set .tmx, .tsx, and .tx files to "Copy if newer."
		//Currently, all .tsx and .tx files must be in the same directory as the main map file, or relative paths break.
		//Image files can be in subfolders.
		//Don't add these files to the MGCB.
		//Images are loaded below using monogame Content loader, so all associated image files should be added to the MGCB.
		//TODO: add runtime image loader.


		//The following is a workaround so if any objects with the Class property are found,
		//an exception isn't thrown if a matching class isn't implemented properly.
		//https://github.com/dcronqvist/DotTiled/issues/42
		string[] classes = [
			"Goblin",
			//"Shape",
		];
		List<CustomClassDefinition> classDefinitions = new();
		foreach(var c in classes) {
			classDefinitions.Add(new CustomClassDefinition { Name = c });
		}

		var shape = new CustomClassDefinition {
			Name = "Shape",
			Members = [ // Make sure that the default values match the Tiled UI
				//new BoolProperty   { Name = "Enabled",        Value = true },
				//new IntProperty    { Name = "MaxSpawnAmount", Value = 10 },
				//new IntProperty    { Name = "MinSpawnAmount", Value = 0 },
				//new StringProperty { Name = "MonsterNames",   Value = "" }
				new ColorProperty  { Name = "FillColor", Value = ColorToColor(Color.Transparent) }
			]
		};
		classDefinitions.Add(shape);

		var loader = Loader.DefaultWith(customTypeDefinitions: classDefinitions);

		//var loader = Loader.Default();
		Map = loader.LoadMap(Path.Combine(content.RootDirectory, path, map_filename));

		TilemapTextures = new();
		TileCollectionTextures = new();
		ImageLayerTextures = new();

		InitTilesets(Map.Tilesets, content, path);
		InitLayerGroup(Map.Layers, content, path);
	}


	private void InitTilesets(List<Tileset> tilesets, ContentManager content, string path) {
		foreach (Tileset tileset in tilesets) {
			if (tileset.Image.HasValue) {
				TilemapTextures.Add(tileset, LoadImage(content, path, tileset.Image));
			} else {
				foreach (Tile tile in tileset.Tiles) {
					TileCollectionTextures.Add(tile, LoadImage(content, path, tile.Image));
				}
			}
		}
	}

	private void InitLayerGroup(List<BaseLayer> layers, ContentManager content, string path) {
		foreach (BaseLayer layer in layers) {
			switch (layer) {
				case Group group:
					InitLayerGroup(group.Layers, content, path);
					break;
				case ImageLayer imagelayer:
					if (!imagelayer.Image.HasValue) break;
					Texture2D image_texture = LoadImage(content, path, imagelayer.Image);
					ImageLayerTextures.Add(imagelayer, image_texture);
					break;
				case TileLayer tilelayer:
					uint[] gids = GetLayerGIDs(tilelayer);
					//uint[] gids = tilelayer.Data.GlobalTileIDs;
					foreach(var gid in gids) {
						if (gid > 1000000) throw new Exception("gid " + gid + " is too large (tiled export glitch)");
					}
					break;
				case ObjectLayer objectlayer: {
					SortObjectLayer(objectlayer, objectlayer.DrawOrder);
					break;
				}

			}
		}
	}


	public static void SortObjectLayer(ObjectLayer layer, DrawOrder drawOrder) {
		switch (drawOrder) {
			case DrawOrder.TopDown:
				layer.Objects.Sort((a, b) => (a.Y.CompareTo(b.Y)));
				break;
			case DrawOrder.Index:
				//no way to "unsort" currently, reloading works.
				break;
		}
	}


	public void Draw(SpriteBatch spritebatch, Point view_offset, Rectangle viewport_bounds) {
		DrawLayerGroup(spritebatch, Map.Layers, view_offset, viewport_bounds, 1);
	}

	public void DrawLayerGroup(SpriteBatch spritebatch, List<BaseLayer> layers, Point view_offset, Rectangle viewport_bounds, float opacity) {
		foreach (BaseLayer layer in layers) {
			Vector2 parallax = LayerParallax(layer);
			Point offset = (view_offset.ToVector2() * parallax).ToPoint();
			switch (layer) {
				case Group group: {
					DrawLayerGroup(spritebatch, group.Layers, (offset) + LayerOffset(group), viewport_bounds, group.Opacity * opacity);
					break;
				}
				case TileLayer tilelayer:
					DrawTileLayer(spritebatch, offset, tilelayer, opacity);
					break;
				case ImageLayer imagelayer:
					DrawImageLayer(spritebatch, offset, imagelayer, viewport_bounds, opacity);
					break;
				case ObjectLayer objectlayer:
					DrawObjectLayer(spritebatch, offset, objectlayer, opacity);
					break;

			}
		}
	}


	public void DrawTileLayer(SpriteBatch spritebatch, Point view_offset, TileLayer layer, float group_opacity) {
		if (!layer.Visible) {
			return;
		}
		uint[] gids = GetLayerGIDs(layer);
		Color tint = ColorToColor(layer.TintColor);
		float opacity = layer.Opacity * group_opacity;

		Vector2 offset_f = new(layer.OffsetX, layer.OffsetY);
		Point offset = offset_f.ToPoint() + view_offset;

		//Issue here with non-CSV, compressed map output
		for (uint i = 0; i < gids.Length; i++) {
			uint gid = gids[i];
			if (gid == 0) continue;
			int x = (int)(i % layer.Width);
			int y = (int)(i / layer.Width);
			(Tileset tileset, Tile tile) = GetTilesetFromGID(gid);

			uint id = gid - tileset.FirstGID;
			Point coord = new(x, y);

			if (tileset.Image.HasValue) {
				DrawTile(spritebatch, id, tileset, coord, offset, tint, opacity);
			} else {
				DrawCollectionTile(spritebatch, tile, tileset, coord, offset, tint, opacity);
			}
		}
	}

	public void DrawObjectLayer(SpriteBatch spritebatch, Point view_offset, ObjectLayer layer, float group_opacity) {
		foreach(var obj in layer.Objects) {
			switch (obj) {
				case TileObject tileobject:
					DrawTileObject(spritebatch, tileobject, layer, view_offset, group_opacity);
					break;
				//The following are very implementation-bound. 
				case PolygonObject:
					break;
				case EllipseObject:
					break;
				case PointObject:
					break;
				case PolylineObject:
					break;
				case RectangleObject:
					break;
				case TextObject:
					break;
			}
		}
	}

	public void DrawImageLayer(SpriteBatch spritebatch, Point view_offset, ImageLayer layer, Rectangle viewport_bounds, float group_opacity) {
		if (!layer.Image.HasValue) return;
		Point layer_offset = LayerOffset(layer);
		Texture2D texture = ImageLayerTextures[layer];
		float opacity = layer.Opacity * group_opacity;
		Color tint = ColorToColor(layer.TintColor);

		//Spritebatch should be using samplerstate with wrap for repeat
		bool repeatX = BoolFromBool(layer.RepeatX, false);
		bool repeatY = BoolFromBool(layer.RepeatY, false);

		//view_offset *= layer.ParallaxX

		Rectangle source_rect;
		Rectangle dest_rect;
		switch (repeatX, repeatY) {
			default:
			case (false, false): {
				source_rect = texture.Bounds;
				dest_rect = new Rectangle(layer_offset + view_offset, new(texture.Width, texture.Height));
				break;
			}
			case (true, true): {
				dest_rect = viewport_bounds;
				source_rect = viewport_bounds;
				source_rect.Location -= view_offset + layer_offset;
				break;
			}
			case (true, false): {
				dest_rect = new Rectangle(new Point(viewport_bounds.X, layer_offset.Y), new Point(viewport_bounds.Width, texture.Height));
				source_rect = new Rectangle(dest_rect.X, 0, dest_rect.Width, texture.Height);
				source_rect.X -= view_offset.X + layer_offset.X;
				dest_rect.Y += view_offset.Y;
				break;
			}
			case (false, true): {
				dest_rect = new Rectangle(new Point(layer_offset.X, viewport_bounds.Y), new Point(texture.Width, viewport_bounds.Height));
				source_rect = new Rectangle(0, dest_rect.Y, texture.Width, dest_rect.Height);
				source_rect.Y -= view_offset.Y + layer_offset.Y;
				dest_rect.X += view_offset.X;
				break;
			}
		}
		spritebatch.Draw(texture, dest_rect, source_rect, tint * opacity);

	}

	/// <summary>
	/// Pi/180 for converting degrees to radians
	/// </summary>
	public const float RAD = 0.01745329f;

	public void DrawTileObject(SpriteBatch spritebatch, TileObject obj, ObjectLayer layer, Point view_offset, float group_opacity) {
		if (!obj.Visible) return;
		const float layerDepth = 1;

 		(Tileset tileset, Tile tile) = GetTilesetFromGID(obj.GID);
		uint id = obj.GID - tileset.FirstGID;

		Color layer_tint = ColorToColor(layer.TintColor);
		Point layer_offset = LayerOffset(layer);
		Point offset = layer_offset + view_offset;
		float opacity = layer.Opacity * group_opacity;
		float rotation = obj.Rotation * RAD;

		Rectangle dest_rect = ObjectBounds(obj);
		dest_rect.Location += offset;

		if (tileset.Image.HasValue) { //Normal tileset tile
			Texture2D texture = TilemapTextures[tileset];
			Rectangle source_rect = GetSourceRect(id, tileset);
			Vector2 origin = new(0, tileset.TileHeight);
			spritebatch.Draw(texture, dest_rect, source_rect, layer_tint * opacity,
				rotation: rotation, origin: origin, SpriteEffects.None, layerDepth);
		} else { //"collection of images" tileset
			Texture2D texture = TileCollectionTextures[tile];
			Rectangle source_rect = TileSourceBounds(tile);
			Vector2 origin = new Vector2(0, tile.Height);
			spritebatch.Draw(texture, dest_rect, source_rect, layer_tint * opacity,
				rotation, origin, SpriteEffects.None, layerDepth);
		}

	}

	


	public void DrawTile(SpriteBatch spritebatch, uint tile_id, Tileset tileset, Point coord, Point offset, Color tint, float opacity) {
		Texture2D texture = TilemapTextures[tileset];
		Rectangle source_rect = GetSourceRect(tile_id, tileset);
		Rectangle dest_rect = new Rectangle(TileSize(tileset) * coord + offset, TileSize(tileset));
		spritebatch.Draw(texture, dest_rect, source_rect, tint * opacity);
	}

	public void DrawCollectionTile(SpriteBatch spritebatch, Tile tile, Tileset tileset, Point coord, Point offset, Color tint, float opacity) {
		//TODO: Tile Render Size, Fill Mode (?), Orientation
		//Object alignment doesn't apply here?

		const float layerDepth = 1.0f;
		Vector2 origin = new Vector2(0, tile.Height);

		Texture2D texture = TileCollectionTextures[tile];
		coord.Y += 1; //image draws from bottom left of map tile
		Point location = coord * TileSize(Map);

		Rectangle dest;
		switch (tileset.RenderSize) {
			case TileRenderSize.Grid:
				dest = new Rectangle(location, TileSize(Map));
				break;
			default:
			case TileRenderSize.Tile:
				dest = new Rectangle(location, TileSize(tile));
				break;
		}
		dest.Location += offset;
		Rectangle source = TileSourceBounds(tile);
		spritebatch.Draw(texture, dest, source, tint * opacity, 0, origin, SpriteEffects.None, layerDepth);
	}






	public static bool BoolFromBool(Optional<bool> optional_bool, bool default_value = false) {
		return optional_bool.HasValue ? optional_bool : default_value;
	}

	/// <summary>
	/// Converts a DotTiled Optional Color to an XNA Color.
	/// </summary>
	/// <param name="default_color">ColorConstants class has matching list of
	/// const UInt32s that can be provided to the Color constructor</param>
	public static Microsoft.Xna.Framework.Color ColorToColor(Optional<DotTiled.Color> dottiled_color, uint default_color = ColorConstants.White) {
		if (dottiled_color.HasValue) {
			var c = dottiled_color.Value;
			return new Color(c.R, c.G, c.B, c.A);
		} else {
			return new Color(default_color);
		}
	}

	/// <summary>
	/// Converts a DotTiled Color to an XNA Color.
	/// </summary>
	public static Microsoft.Xna.Framework.Color ColorToColor(DotTiled.Color c) {
		return new Color(c.R, c.G, c.B, c.A);
	}

	/// <summary>
	/// Converts XNA Color to Dottiled Color
	/// </summary>
	public static DotTiled.Color ColorToColor(Microsoft.Xna.Framework.Color c) {
		return new DotTiled.Color() {
			R = c.R,
			G = c.G,
			B = c.B,
			A = c.A
		};
	}


	public static Rectangle GetSourceRect(uint id, Tileset tileset) {
		uint col = id % tileset.Columns;
		uint row = id / tileset.Columns;

		uint x = tileset.Margin + (col * (tileset.TileWidth + tileset.Spacing));
		uint y = tileset.Margin + (row * (tileset.TileHeight + tileset.Spacing));

		return new Rectangle((int)x, (int)y, (int)tileset.TileWidth, (int)tileset.TileHeight);
	}



	public (Tileset tileset, Tile tile) GetTilesetFromGID(uint gid) {
		foreach (Tileset tileset in Map.Tilesets) {
			if (tileset.Image.HasValue) {
				if (gid >= tileset.FirstGID && gid < tileset.FirstGID + tileset.TileCount) {
					return (tileset, null);
				}
			} else {
				//Collection tilesets can have missing IDs and IDs greater than the tile count
				foreach(Tile tile in tileset.Tiles) {
					if (tile.ID == gid - tileset.FirstGID) {
						return (tileset, tile);
					}
				}
			}

		}
		throw new Exception("gid " + gid + " has a problem");
	}

	public static uint[] GetLayerGIDs(TileLayer layer) {
		//if(layer.Data.HasValue) {
			Data data = layer.Data;
			return data.GlobalTileIDs;
		//} else {
		//	return Array.Empty<uint>();
		//}
	}

	public static Texture2D LoadImage(ContentManager content, string content_subfolder, Image image) {
		string relative_path = image.Source;
		string file = Path.GetFileNameWithoutExtension(relative_path);
		string folder = Path.GetDirectoryName(relative_path);
		string path = Path.Combine(content_subfolder, folder, file);
		return content.Load<Texture2D>(path);
	}

}