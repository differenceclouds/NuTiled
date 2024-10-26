using System;
using System.Collections.Generic;
using DotTiled;
using DotTiled.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using Color = Microsoft.Xna.Framework.Color;

namespace NuTiled;


public class TiledMap {

	public Map Map { get; }

	public Texture2D[] TilemapTextures { get; }
	//public Texture2D[] ImageLayerTextures { get; }
	public Dictionary<ImageLayer, Texture2D> ImageLayerTextures { get; }
	public TiledMap(ContentManager content, string path) {
		Map = LoadMap(path);
		TilemapTextures = new Texture2D[Map.Tilesets.Count];
		ImageLayerTextures = new();

		for(int i = 0; i < Map.Tilesets.Count; i++) {
			Tileset tileset = Map.Tilesets[i];
			TilemapTextures[i] = LoadImage(content, "tiled", tileset.Image); ;
		}
		foreach(BaseLayer layer in Map.Layers) {
			switch (layer) {
				case ImageLayer imagelayer:
					if (!imagelayer.Image.HasValue) break;
					Texture2D image_texture = LoadImage(content, "tiled", imagelayer.Image);
					ImageLayerTextures.Add(imagelayer, image_texture);
					break;
				case TileLayer tilelayer:
					break;
			}
		}
	}

	public Color BackgroundColor => ColorFromColor(Map.BackgroundColor);

	public void Draw(SpriteBatch spriteBatch, Rectangle viewport_bounds) {
		foreach (BaseLayer layer in Map.Layers) {
			switch (layer) {
				case TileLayer tileLayer:
					DrawTileLayer(spriteBatch, tileLayer);
					break;
				case ImageLayer imageLayer when imageLayer.Image.HasValue:
					DrawImageLayer(spriteBatch, imageLayer, viewport_bounds);
					break;

			}
		}
	}


	public void DrawTileLayer(SpriteBatch spriteBatch, TileLayer layer) {
		if (!layer.Visible) {
			return;
		}
		uint[] gids = GetLayerGIDs(layer);
		Color tint = ColorFromColor(layer.TintColor);

		Vector2 offset_f = new(layer.OffsetX, layer.OffsetY);
		Point offset = offset_f.ToPoint();

		for (int y = 0; y < layer.Height; y++) {
			for (int x = 0; x < layer.Width; x++) {
				uint gid = gids[y * layer.Width + x];
				if (gid == 0) continue;

				Tileset tileset = GetTilesetFromGID(Map, gid);
				uint id = gid - tileset.FirstGID;

				int tileset_index = Map.Tilesets.IndexOf(tileset);
				Texture2D texture = TilemapTextures[tileset_index];
				Rectangle source_rect = GetTilesetSourceRect(id, tileset);

				Rectangle dest_rect = new Rectangle(x * (int)tileset.TileWidth, y * (int)tileset.TileHeight, (int)tileset.TileWidth, (int)tileset.TileHeight);
				dest_rect.Location += offset;
				spriteBatch.Draw(texture, dest_rect, source_rect, tint * layer.Opacity);
			}
		}
	}

	public void DrawImageLayer(SpriteBatch spriteBatch, ImageLayer layer, Rectangle viewport_bounds) {
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
				spriteBatch.Draw(texture, rect, tint * opacity);
				break;
			}
			case (true, true): {
				Rectangle dest_rect = viewport_bounds;
				Rectangle source_rect = viewport_bounds;
				source_rect.Offset(new Point(offset_x, offset_y));
				spriteBatch.Draw(texture, dest_rect, source_rect, tint * opacity);
				break;
			}
			case (true, false): {
				Rectangle dest_rect = new Rectangle(viewport_bounds.X, offset_y, viewport_bounds.Width, texture.Height);
				Rectangle source_rect = new Rectangle(dest_rect.X, 0, dest_rect.Width, texture.Height);
				spriteBatch.Draw(texture, dest_rect, source_rect, tint * opacity);
				break;
			}
			case (false, true): {
				Rectangle dest_rect = new Rectangle(offset_x, viewport_bounds.Y, texture.Width, viewport_bounds.Height);
				Rectangle source_rect = new Rectangle(0, dest_rect.Y, texture.Width, dest_rect.Height);
				spriteBatch.Draw(texture, dest_rect, source_rect, tint * opacity);
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



	public static Tileset GetTilesetFromGID(Map map, uint gid) {
		foreach (Tileset tileset in map.Tilesets) {
			if (gid >= tileset.FirstGID && gid < tileset.FirstGID + tileset.TileCount) {
				return tileset;
			}
		}
		return null;
	}


	public static Map LoadMap(string path) {
		var loader = Loader.Default();
		return loader.LoadMap(path);
	}

	public static uint[] GetLayerGIDs(TileLayer layer) {
		Data data = layer.Data;
		return data.GlobalTileIDs;
	}

	//public static string GetTilesetImagePath(Tileset tileset) {
	//	Image image = tileset.Image;
	//	return image.Source;
	//}

	//public static Texture2D LoadTilesetTexture(ContentManager content, string content_subfolder, Tileset tileset) {
	//	string relative_path = GetTilesetImagePath(tileset);
	//	string file = Path.GetFileNameWithoutExtension(relative_path);
	//	string folder = Path.GetDirectoryName(relative_path);
	//	string path = Path.Combine(content_subfolder, folder, file);
	//	return content.Load<Texture2D>(path);
	//}

	public static Texture2D LoadImage(ContentManager content, string content_subfolder, Image image) {
		string relative_path = image.Source;
		string file = Path.GetFileNameWithoutExtension(relative_path);
		string folder = Path.GetDirectoryName(relative_path);
		string path = Path.Combine(content_subfolder, folder, file);
		return content.Load<Texture2D>(path);
	}

}