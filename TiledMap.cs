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


	//Trying to keep instance properties to a minimum and not make redundant classes.
	public Map Map { get; }
	public Dictionary<Tileset, Texture2D> TilemapTextures { get; }
	public Dictionary<Tile, Texture2D> CollectionTextures { get; }
	public Dictionary<ImageLayer, Texture2D> ImageLayerTextures { get; }
	public TiledMap(ContentManager content, string path, string map_filename) {

		//Using DotTiled's loader. Set .tmx, .tsx, and .tx files to "Copy if newer."
		//Currently, all .tsx and .tx files must be in the same directory as the main map file, or relative paths break
		//Image files can be in subfolders.
		//Don't add these files to the MGCB.
		//Images are loaded below using monogame Content loader,
		//so all associated images should be added to the MGCB.
		//TODO: add runtime image loader.


		//The following is a workaround so if any objects with the Class property are found,
		//an exception isn't thrown if a matching class isn't implemented properly.
		//https://github.com/dcronqvist/DotTiled/issues/42
		string[] classes = [
			"Goblin",
			"Shape",
		];
		var classDefinitions = classes.Select(c => new CustomClassDefinition { Name = c });
		var loader = Loader.DefaultWith(customTypeDefinitions: classDefinitions);


		//var loader = Loader.Default();
		Map = loader.LoadMap(Path.Combine(content.RootDirectory, path, map_filename));


		TilemapTextures = new();
		CollectionTextures = new();
		ImageLayerTextures = new();

		foreach(Tileset tileset in Map.Tilesets) {
			if (tileset.Image.HasValue) {
				TilemapTextures.Add(tileset, LoadImage(content, path, tileset.Image));
			} else {
				foreach (Tile tile in tileset.Tiles) {
					CollectionTextures.Add(tile, LoadImage(content, path, tile.Image));
				}
			}
		}

		foreach(BaseLayer layer in Map.Layers) {
			switch (layer) {
				case ImageLayer imagelayer:
					if (!imagelayer.Image.HasValue) break;
					Texture2D image_texture = LoadImage(content, path, imagelayer.Image);
					ImageLayerTextures.Add(imagelayer, image_texture);
					break;
				case TileLayer tilelayer:
					break;
			}
		}
	}



	public Color BackgroundColor => ColorFromColor(Map.BackgroundColor);

	public void Draw(SpriteBatch spritebatch, Rectangle viewport_bounds) {
		foreach (BaseLayer layer in Map.Layers) {
			switch (layer) {
				case TileLayer tilelayer:
					DrawTileLayer(spritebatch, tilelayer);
					break;
				case ImageLayer imagelayer:
					DrawImageLayer(spritebatch, imagelayer, viewport_bounds);
					break;
				case ObjectLayer objectlayer:
					DrawObjectLayer(spritebatch, objectlayer);
					break;

			}
		}
	}

	public void DrawObjectLayer(SpriteBatch spritebatch, ObjectLayer layer) {
		Point offset = new((int)layer.OffsetX, (int)layer.OffsetY);
		foreach(var obj in layer.Objects) {
			switch (obj) {
				case TileObject tileobject:
					DrawTileObject(spritebatch, tileobject, layer);
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

	public const float RAD = 0.01745329f;

	public void DrawTileObject(SpriteBatch spritebatch, TileObject obj, ObjectLayer layer) {
		if (!obj.Visible) return;
 		(Tileset tileset, Tile tile) = GetTilesetFromGID(obj.GID);
		uint id = obj.GID - tileset.FirstGID;

		Color layer_tint = ColorFromColor(layer.TintColor);
		Point layer_offset = new Point((int)layer.OffsetX, (int)layer.OffsetY);
		float layer_opacity = layer.Opacity;
		float rotation = obj.Rotation * RAD;

		if(tileset.Image.HasValue) {
			Texture2D texture = TilemapTextures[tileset];
			Rectangle source_rect = GetTilesetSourceRect(id, tileset);
			Point location = new Point((int)obj.X, (int)obj.Y) + layer_offset;
			Point size = new((int)obj.Width, (int)obj.Height);
			Rectangle dest_rect = new Rectangle(location, size);
			Vector2 origin = new Vector2(0, tileset.TileHeight);
			spritebatch.Draw(texture, dest_rect, source_rect, layer_tint * layer_opacity, rotation: rotation, origin: origin, SpriteEffects.None, 1);
		} else {
			Texture2D texture = CollectionTextures[tile];
			Rectangle source_rect = new Rectangle((int)tile.X, (int)tile.Y, (int)tile.Width, (int)tile.Height);
			Point location = new Point((int)obj.X, (int)obj.Y) + layer_offset;
			Point size = new Point((int)obj.Width, (int)obj.Height);
			Rectangle dest_rect = new Rectangle(location, size);
			Vector2 origin = new Vector2(0, tile.Height);
			spritebatch.Draw(texture, dest_rect, source_rect, layer_tint * layer_opacity, rotation, origin, SpriteEffects.None, 1);
		}

	}

	

	public void DrawTileLayer(SpriteBatch spritebatch, TileLayer layer) {
		if (!layer.Visible) {
			return;
		}
		uint[] gids = GetLayerGIDs(layer);
		Color tint = ColorFromColor(layer.TintColor);

		Vector2 offset_f = new(layer.OffsetX, layer.OffsetY);
		Point offset = offset_f.ToPoint();

		//could iterate over gids[], calculating x and y instead.
		for (int y = 0; y < layer.Height; y++) {
			for (int x = 0; x < layer.Width; x++) {
				uint gid = gids[y * layer.Width + x];
				if (gid == 0) continue;

				//could be more efficient by counting tilesets used on layer,
				//and avoiding the following function is there is only one tileset used.
				//Or, creating a huge Dictionary gid,tileset on map load?
				(Tileset tileset, Tile tile) = GetTilesetFromGID(gid);

				uint id = gid - tileset.FirstGID;

				if(tileset.Image.HasValue) {
					Texture2D texture = TilemapTextures[tileset];

					Rectangle source_rect = GetTilesetSourceRect(id, tileset);
					Rectangle dest_rect = new Rectangle(x * (int)tileset.TileWidth, y * (int)tileset.TileHeight, (int)tileset.TileWidth, (int)tileset.TileHeight);

					dest_rect.Location += offset;
					spritebatch.Draw(texture, dest_rect, source_rect, tint * layer.Opacity);
				} else {
					Texture2D texture = CollectionTextures[tile];
					spritebatch.Draw(texture, new Vector2(x * 64, y * 64), tint * layer.Opacity);
				}

			}
		}
	}


	public void DrawImageLayer(SpriteBatch spritebatch, ImageLayer layer, Rectangle viewport_bounds) {
		if (!layer.Image.HasValue) return;

		Texture2D texture = ImageLayerTextures[layer];
		int offset_x = (int)layer.OffsetX;
		int offset_y = (int)layer.OffsetY;
		float opacity = layer.Opacity;
		Color tint = ColorFromColor(layer.TintColor);
		Rectangle rect = new Rectangle(offset_x, offset_y, texture.Width, texture.Height);

		//Spritebatch should be using samplerstate with wrap for repeat
		bool repeatX = BoolFromBool(layer.RepeatX, false);
		bool repeatY = BoolFromBool(layer.RepeatY, false);
		switch (repeatX, repeatY) {
			case (false, false): {
				spritebatch.Draw(texture, rect, tint * opacity);
				break;
			}
			case (true, true): {
				Rectangle dest_rect = viewport_bounds;
				Rectangle source_rect = viewport_bounds;
				source_rect.Offset(new Point(offset_x, offset_y));
				spritebatch.Draw(texture, dest_rect, source_rect, tint * opacity);
				break;
			}
			case (true, false): {
				Rectangle dest_rect = new Rectangle(viewport_bounds.X, offset_y, viewport_bounds.Width, texture.Height);
				Rectangle source_rect = new Rectangle(dest_rect.X, 0, dest_rect.Width, texture.Height);
				spritebatch.Draw(texture, dest_rect, source_rect, tint * opacity);
				break;
			}
			case (false, true): {
				Rectangle dest_rect = new Rectangle(offset_x, viewport_bounds.Y, texture.Width, viewport_bounds.Height);
				Rectangle source_rect = new Rectangle(0, dest_rect.Y, texture.Width, dest_rect.Height);
				spritebatch.Draw(texture, dest_rect, source_rect, tint * opacity);
				break;
			}
		}

	}



	public static bool BoolFromBool(Optional<bool> dottiled_bool, bool default_value = false) {
		return dottiled_bool.HasValue ? dottiled_bool : default_value;
	}

	/// <summary>
	/// Converts a DotTiled Optional Color to an XNA Color.
	/// </summary>
	/// <param name="default_color">ColorConstants class has matching list of const UInt32s that can be provided to the Color constructor</param>
	/// <returns></returns>
	public static Color ColorFromColor(Optional<DotTiled.Color> dottiled_color, UInt32 default_color = ColorConstants.White) {
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
	/// <param name="c"></param>
	/// <returns></returns>
	public static Color ColorFromColor(DotTiled.Color c) {
		return new Color(c.R, c.G, c.B, c.A);
	}


	public static Rectangle GetTilesetSourceRect(uint id, Tileset tileset) {
		uint col = id % tileset.Columns;
		uint row = id / tileset.Columns;

		uint x = tileset.Margin + (col * (tileset.TileWidth + tileset.Spacing));
		uint y = tileset.Margin + (row * (tileset.TileHeight + tileset.Spacing));

		return new Rectangle((int)x, (int)y, (int)tileset.TileWidth, (int)tileset.TileHeight);
	}



	public (Tileset tileset, Tile tile) GetTilesetFromGID(uint gid) {
		if (gid > 1000000) throw new Exception("gid " + gid + " is too large (tiled export glitch)");

		foreach (Tileset tileset in Map.Tilesets) {
			if (tileset.Image.HasValue) {
				if (gid >= tileset.FirstGID && gid <= tileset.FirstGID + tileset.TileCount) {
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

		//diagnostic:
		foreach(Tileset tileset in Map.Tilesets) {
			Console.WriteLine("tileset: "+tileset.Name+" firstgid: "+tileset.FirstGID+" tilecount: "+tileset.TileCount);
		}
		Console.WriteLine();
		throw new Exception("gid " + gid + " has a problem");
	}

	public static uint[] GetLayerGIDs(TileLayer layer) {
		Data data = layer.Data;
		return data.GlobalTileIDs;
	}

	public static Texture2D LoadImage(ContentManager content, string content_subfolder, Image image) {
		string relative_path = image.Source;
		string file = Path.GetFileNameWithoutExtension(relative_path);
		string folder = Path.GetDirectoryName(relative_path);
		string path = Path.Combine(content_subfolder, folder, file);
		return content.Load<Texture2D>(path);
	}

}