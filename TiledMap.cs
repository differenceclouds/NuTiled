using System;
using System.Collections.Generic;
using System.IO;
using DotTiled;
using DotTiled.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace NuTiled;
public class TiledMap {
	//Trying to keep instance properties to a minimum and not make redundant classes that copy data.
	//Rendering is presently very "immediate mode," with no data being retained other than from the loaded Dottiled classes.
	//Planning to do a version where some converted data is retained and compare performance.

	//Most positional float values are cast to int.
	//this step could be pushed forward to the spritebatch.draw calls by using Vector2s instead of Points everywhere, for higher accuracy.
	//In this case, a RectangleF struct (like monogame extended) could be introduced.

	public Map Map { get; }

	//ContentManager content;
	GraphicsDevice graphicsDevice;

	//map file path, relative to project directory
	//ex: "levels/level01.tmx" if full path would be "Content/tiled/levels/level01.tmx"
	//note: keep folder structure of .tiled-project
	public string MapFilePath { get; }

	//.tiled-project directory, relative to executing root
	//ex: "Content/tiled"
	public string TiledProjectDirectory { get; }
	public string MapFileDirectory => Path.Combine(TiledProjectDirectory, Path.GetDirectoryName(MapFilePath));
	public uint Height => Map.Height;
	public uint Width => Map.Width;
	public uint HeightInPixels => Map.Height * Map.TileHeight;
	public uint WidthInPixels => Map.Width * Map.TileWidth;
	public Color BackgroundColor => ColorToColor(Map.BackgroundColor);


	#region class-required dictionaries
	public Dictionary<Tileset, Texture2D> TilemapTextures { get; } = new();
	public Dictionary<Tile, Texture2D> TileCollectionTextures { get; } = new();
	public Dictionary<ImageLayer, Texture2D> ImageLayerTextures { get; } = new();
	
	//This and following can be replaced by GetTilesetFromGid method where used. Unsure which is faster.
	//All GIDs up to the max for the map are covered, with nulls where applicable.
	public Dictionary<uint, Tileset> TilesetsByGID { get; } = new();
	public Dictionary<uint, Tile> CollectionTilesByGID { get; } = new();
	#endregion

	#region optional shortcut dictionaries
	//populated on instance construction
	public Dictionary<string, Tileset> TilesetsByName { get; } = new();
	public Dictionary<string, BaseLayer> AllLayersByName { get; } = new(); //This is all you need of the following if you don't mind casting to the other layer types.
	public Dictionary<string, TileLayer> TileLayersByName { get; } = new();
	public Dictionary<string, ObjectLayer> ObjectLayersByName { get; } = new();
	public Dictionary<string, Group> LayerGroupsByName { get; } = new();
	public Dictionary<string, ImageLayer> ImageLayersByName { get; } = new();
	#endregion


	public readonly List<ICustomTypeDefinition> CustomTypeDefinitions = new();

	//dictionary between object and custom class. this probably should be in another file.
	Dictionary<DotTiled.Object, GameClasses.FilledShape> FilledShapes = new();

	public TiledMap(GraphicsDevice graphicsDevice, string projectDirectory, string mapFilePath, List<ICustomTypeDefinition> typeDefinitions) {
		this.graphicsDevice = graphicsDevice;
		//MapFile = Path.GetFileName(path);
		//TiledProjectDirectory = Path.GetDirectoryName(path);
		MapFilePath = mapFilePath;
		TiledProjectDirectory = projectDirectory;
		foreach (var t in typeDefinitions) {
			CustomTypeDefinitions.Add(t);
		}

		//Using DotTiled's loader.
		//Set .tmx, .tsx, and .tx files to "Copy if newer." 
		//Currently, all .tsx and .tx files must be in the same directory as the main map file, or relative paths break.
		//Image files can be in subfolders.

		var loader = Loader.DefaultWith(customTypeDefinitions: typeDefinitions);

		Map = loader.LoadMap(Path.Combine(TiledProjectDirectory, MapFilePath));

		InitTilesets(Map.Tilesets, TiledProjectDirectory);
		InitLayerGroup(Map.Layers, TiledProjectDirectory);
	}

	
	#region Tile manipulation functions
	public uint GetTileGID(TileLayer tileLayer, Point coord) {
		var gids = GetLayerGIDs(tileLayer);
		return gids[coord.Y * tileLayer.Width + coord.X];
	}

	/// <summary>
	/// The highest gid, plus one
	/// </summary>
	public uint GID_Count {
		get {
			var last_tileset = Map.Tilesets[Map.Tilesets.Count - 1];
			return last_tileset.FirstGID + last_tileset.TileCount;
		}
	}

	public (Tileset tileset, uint tileID) GetTileID(TileLayer tileLayer, Point coord) {
		var gid = GetTileGID(tileLayer, coord);
		//var tileset = GetTilesetFromGID(gid).tileset;
		var tileset = TilesetsByGID[gid];
		return (tileset, gid - tileset.FirstGID);
	}

	/// <summary>
	/// Set tile by GlobalTileID
	/// </summary>
	public void SetTile(TileLayer layer, Point coord, uint GID) {
		Data data = layer.Data;
		uint[] gids = data.GlobalTileIDs;
		gids[coord.Y * layer.Width + coord.X] = GID;
	}

	/// <summary>
	/// Set tile by local tileset ID.
	/// </summary>
	/// <param name="wrap_id">If true, out of bounds IDs will wrap around the tileset</param>
	public void SetTileByID(TileLayer layer, Point coord, Tileset tileset, uint tileID, bool wrap_id = true) {
		if(wrap_id) {
			tileID = tileID % tileset.TileCount;
		}
		uint GID = tileset.FirstGID + tileID;
		SetTile(layer, coord, GID);
	}

	public void PlaceTileObject(ObjectLayer layer, TileObject tile) {
		layer.Objects.Add(tile);
	}
	public void PlaceTileObject(ObjectLayer layer, Vector2 position, uint GID) {
		
	}

	#endregion
	
	#region map initialization functions
	private void InitTilesets(List<Tileset> tilesets, string path) {
		foreach (Tileset tileset in tilesets) {
			TilesetsByName.Add(tileset.Name, tileset);
			if (tileset.Image.HasValue) {
				Image image = tileset.Image;
				string tileset_path = tileset.Source;
				TilemapTextures.Add(tileset, LoadImage(graphicsDevice, path, tileset.Image));
			} else {
				foreach (Tile tile in tileset.Tiles) {
					TileCollectionTextures.Add(tile, LoadImage(graphicsDevice, path, tile.Image));
				}
			}
		}

		//hit every number to add nulls to dictionaries
		for (uint gid = 1; gid < GID_Count; gid++) {
			var (tileset, tile) = GetTilesetFromGID(gid, true);
			TilesetsByGID.Add(gid, tileset);
			CollectionTilesByGID.Add(gid, tile);
		}

	}

	private void InitLayerGroup(List<BaseLayer> layers, string path) {
		foreach (BaseLayer layer in layers) {
			AllLayersByName.Add(layer.Name, layer);
			switch (layer) {
				case Group group:
					LayerGroupsByName.Add(group.Name, group);
					InitLayerGroup(group.Layers, path);
					break;
				case ImageLayer imagelayer:
					ImageLayersByName.Add(imagelayer.Name, imagelayer);
					if (!imagelayer.Image.HasValue) break;
					Texture2D image_texture = LoadImage(graphicsDevice, MapFileDirectory, imagelayer.Image);
					ImageLayerTextures.Add(imagelayer, image_texture);
					break;
				case TileLayer tilelayer:
					TileLayersByName.Add(tilelayer.Name, tilelayer);
					Data data = tilelayer.Data;
					uint[] gids = data.GlobalTileIDs;
					foreach (var gid in gids) {
						if (gid > 100000) throw new Exception("gid " + gid + " is too large (tiled export glitch, probably flip related)");
					}
					break;
				case ObjectLayer objectlayer:
					ObjectLayersByName.Add(objectlayer.Name, objectlayer);
					SortObjectLayer(objectlayer, objectlayer.DrawOrder);
					InitObjectLayer(objectlayer);
					break;
			}
		}
	}

	private void InitObjectLayer(ObjectLayer layer) {
		foreach (var obj in layer.Objects) {
			//Parse out custom types
			switch (obj.Type) {
				case "FilledShape": {
					var shape = obj.MapPropertiesTo<CustomTypes.FilledShape>();
					FilledShapes.Add(obj, new GameClasses.FilledShape(shape));
				}
				break;
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
	#endregion
	
	#region Drawing functions
	public void Draw(SpriteBatch spritebatch, Point view_offset, Rectangle viewport_bounds) {
		DrawLayerGroup(spritebatch, Map.Layers, view_offset, viewport_bounds, 1);
		if(DrawGrid) {
			DrawGridd(spritebatch, view_offset.ToVector2(), GridColor);

		}
	}

	void DrawGridd(SpriteBatch spritebatch, Vector2 offset, Color color) {
		Color gridcolor = GridColor;
		for (int y = 0; y <= HeightInPixels; y += (int)Map.TileHeight) {
			Primitives2D.DrawLine(spritebatch, new Vector2(0, y) + offset, new Vector2(WidthInPixels, y) + offset, gridcolor);
		}
		for (int x = 0; x <= WidthInPixels; x += (int)Map.TileWidth) {
			Primitives2D.DrawLine(spritebatch, new Vector2(x, 0) + offset, new Vector2(x, HeightInPixels) + offset, gridcolor);
		}
	}
	public static bool DrawGrid { get; set; }
	public static Color GridColor { get; set; } = Color.Gray * 0.5f;

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
		Data data = layer.Data;
		uint[] gids = data.GlobalTileIDs;
		FlippingFlags[] flips = data.FlippingFlags;
		
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

			//(Tileset tileset, Tile tile) = GetTilesetFromGID(gid);
			Tileset tileset = TilesetsByGID[gid];

			uint id = gid - tileset.FirstGID;
			Point coord = new(x, y);

			//TODO: tile rotations
			var flipX = flips[i].HasFlag(FlippingFlags.FlippedHorizontally) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			var flipY = flips[i].HasFlag(FlippingFlags.FlippedVertically) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			SpriteEffects flip = flipX | flipY;

			if (tileset.Image.HasValue) {
				DrawTile(spritebatch, id, tileset, coord, offset, tint, opacity, flip);
			} else {
				var tile = CollectionTilesByGID[gid];
				DrawCollectionTile(spritebatch, tile, tileset, coord, offset, tint, opacity, flip);
			}
		}
	}

	public void DrawObjectLayer(SpriteBatch spritebatch, Point view_offset, ObjectLayer layer, float group_opacity) {
		if (!layer.Visible) {
			return;
		}
		foreach (var obj in layer.Objects) {
			//custom object draw functions
			if(obj.Type == "FilledShape") { //there is probably better way to do this
				FilledShapes[obj].Draw(spritebatch, obj, view_offset);
				continue;
			}
			//generic draw functions
			switch (obj) {
				case TileObject tileobject:
					DrawTileObject(spritebatch, tileobject, layer, view_offset, group_opacity);
					break;
				case PolygonObject polygon:
					break;
				case EllipseObject ellipse:
					break;
				case PointObject point:
					break;
				case PolylineObject polyline:
					break;
				case RectangleObject rect:
					break;
				case TextObject text:
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
	const float RAD = 0.01745329f;

	public void DrawTileObject(SpriteBatch spritebatch, TileObject obj, ObjectLayer layer, Point view_offset, float group_opacity) {
		if (!obj.Visible) return;
		const float layerDepth = 1;

		//(Tileset tileset, Tile tile) = GetTilesetFromGID(obj.GID);
		Tileset tileset = TilesetsByGID[obj.GID];
		uint id = obj.GID - tileset.FirstGID;


		Color layer_tint = ColorToColor(layer.TintColor);
		Point layer_offset = LayerOffset(layer);
		Point offset = layer_offset + view_offset;
		float opacity = layer.Opacity * group_opacity;
		float rotation = obj.Rotation * RAD;

		Rectangle dest_rect = GetObjectBounds(obj);
		dest_rect.Location += offset;

		if (tileset.Image.HasValue) { //Normal tileset tile
			Texture2D texture = TilemapTextures[tileset];
			Rectangle source_rect = GetSourceRect(id, tileset);
			Vector2 origin = new(0, tileset.TileHeight);
			spritebatch.Draw(texture, dest_rect, source_rect, layer_tint * opacity,
				rotation: rotation, origin: origin, SpriteEffects.None, layerDepth);
		} else { //"collection of images" tileset
			Tile tile = CollectionTilesByGID[obj.GID];
			Texture2D texture = TileCollectionTextures[tile];
			Rectangle source_rect = TileSourceBounds(tile);
			Vector2 origin = new Vector2(0, tile.Height);
			spritebatch.Draw(texture, dest_rect, source_rect, layer_tint * opacity,
				rotation, origin, SpriteEffects.None, layerDepth);
		}
	}

	


	public void DrawTile(SpriteBatch spritebatch, uint tile_id, Tileset tileset, Point coord, Point offset, Color tint, float opacity, SpriteEffects flip = SpriteEffects.None) {
		Texture2D texture = TilemapTextures[tileset];
		Rectangle source_rect = GetSourceRect(tile_id, tileset);
		Point tile_size;
		switch (tileset.RenderSize) {
			default:
			case TileRenderSize.Tile:
				tile_size = TileSize(tileset);
				break;
			case TileRenderSize.Grid:
				tile_size = TileSize(Map);
				break;
		}
		Rectangle dest_rect = new Rectangle(TileSize(Map) * coord + offset, tile_size);
		spritebatch.Draw(texture, dest_rect, source_rect, tint * opacity, 0,Vector2.Zero,flip, 1);
	}

	public void DrawCollectionTile(SpriteBatch spritebatch, Tile tile, Tileset tileset, Point coord, Point offset, Color tint, float opacity, SpriteEffects flip = SpriteEffects.None) {
		//TODO: Fill Mode (?), Orientation
		//Object alignment doesn't apply here?

		const float layerDepth = 1.0f;
		Vector2 origin = new Vector2(0, tile.Height);

		Texture2D texture = TileCollectionTextures[tile];
		coord.Y += 1; //image draws from bottom left of map tile
		Point location = coord * TileSize(Map);
		Rectangle source = TileSourceBounds(tile);
		Rectangle dest;
		switch (tileset.RenderSize) {
			case TileRenderSize.Grid:
				dest = new Rectangle(location, TileSize(Map));
				//assuming map grid size is square
				if (tileset.FillMode == DotTiled.FillMode.PreserveAspectFit && TileSize(tile).X != TileSize(tile).Y) {
					if (TileSize(tile).Y < TileSize(tile).X) {
						float v_aspect_ratio = (float)tile.Height / (float)tile.Width;
						dest.Height = (int)(dest.Height * v_aspect_ratio);
						dest.Y -= (TileSize(Map).Y - dest.Height) / 2;
					} else {
						float h_aspect_ratio = (float)tile.Width / (float)tile.Height;
						dest.Width = (int)(dest.Width * h_aspect_ratio);
						dest.X += (TileSize(Map).X - dest.Width) / 2;
					}
				}
				break;
			default:
			case TileRenderSize.Tile:
				dest = new Rectangle(location, TileSize(tile));
				break;
		}
		dest.Location += offset;
		spritebatch.Draw(texture, dest, source, tint * opacity, 0, origin, flip, layerDepth);
	}
	#endregion
	
	#region data retrieval functions



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
	public static Rectangle GetObjectBounds(DotTiled.Object obj) {
		return new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height);
	}

	public static Rectangle GetSourceRect(uint id, Tileset tileset) {
		uint col = id % tileset.Columns;
		uint row = id / tileset.Columns;

		uint x = tileset.Margin + (col * (tileset.TileWidth + tileset.Spacing));
		uint y = tileset.Margin + (row * (tileset.TileHeight + tileset.Spacing));

		return new Rectangle((int)x, (int)y, (int)tileset.TileWidth, (int)tileset.TileHeight);
	}


	/// <summary>
	/// Returns the Tileset, and a Tile object if the tileset is a collection of images. Specify "safe" to return null on invalid GID.
	/// </summary>
	public (Tileset tileset, Tile tile) GetTilesetFromGID(uint gid, bool safe = false) {
		foreach (Tileset tileset in Map.Tilesets) {
			if (tileset.Image.HasValue) {
				if (gid >= tileset.FirstGID && gid < tileset.FirstGID + tileset.TileCount) {
					return (tileset, null);
				}
			} else {
				//Collection tilesets can have missing IDs and IDs greater than the tile count
				
				foreach (Tile tile in tileset.Tiles) {
					if (tile.ID == gid - tileset.FirstGID) {
						return (tileset, tile);
					}
				}
			}
		}
		if(!safe) {
			if (gid < 1000000)
				throw new Exception($"No tileset with gid {gid} exists.");
			else
				throw new Exception($"gid {gid} doesn't have FlippingFlag bits stripped");
		} else {
			return (null, null);
		}
	}




	public static uint[] GetLayerGIDs(TileLayer layer) {
		Data data = layer.Data;
		return data.GlobalTileIDs;
	}

	public static FlippingFlags[] GetFlippingFlags(TileLayer layer) {
		Data data = layer.Data;
		return data.FlippingFlags;
	}
	#endregion
	
	#region asset loading functions
	/// <param name="caller_directory">The directory that the XML file referencing this image is in</param>
	public static Texture2D LoadImage(GraphicsDevice graphicsDevice, string caller_directory, Image image) {
		string relative_path = image.Source;
		string file = Path.GetFileName(relative_path);
		string folder = Path.GetDirectoryName(relative_path);
		string path = Path.Combine(caller_directory, folder, file);

		//TODO: prevent redundant texture reloading. Slowest step of reload by far.
		return LoadImage(graphicsDevice, path);
	}

	public static Texture2D LoadImage(GraphicsDevice graphicsDevice, string path) {
		return Texture2D.FromFile(graphicsDevice, path, DefaultColorProcessors.PremultiplyAlpha);
	}
	#endregion
	
	#region conversion helper functions
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
			return new Microsoft.Xna.Framework.Color(c.R, c.G, c.B, c.A);
		} else {
			return new Microsoft.Xna.Framework.Color(default_color);
		}
	}

	/// <summary>
	/// Converts between XNA Color and Dottiled color, depending on overload.
	/// </summary>
	public static Microsoft.Xna.Framework.Color ColorToColor(DotTiled.Color c) {
		return new Microsoft.Xna.Framework.Color(c.R, c.G, c.B, c.A);
	}


	/// <summary>
	/// Converts between XNA Color and Dottiled color, depending on overload.
	/// </summary>
	public static DotTiled.Color ColorToColor(Microsoft.Xna.Framework.Color c) {
		return new DotTiled.Color() {
			R = c.R,
			G = c.G,
			B = c.B,
			A = c.A
		};
	}


	#endregion
	
}