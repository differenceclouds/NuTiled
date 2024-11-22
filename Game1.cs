using DotTiled;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using Color = Microsoft.Xna.Framework.Color;

namespace NuTiled;
public class Game1 : Game {
	private GraphicsDeviceManager graphics;
	private SpriteBatch spriteBatch;


	TiledMap tiledMap;
	ContentReloader contentReloader;

	public Game1() {
		graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
	}

	protected override void Initialize() {
		//int w = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
		//int h = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
		graphics.PreferredBackBufferWidth = 1024;
		graphics.PreferredBackBufferHeight = 768;
		graphics.IsFullScreen = false;
		graphics.ApplyChanges();
		base.Initialize();
	}

	protected override void LoadContent() {
		spriteBatch = new SpriteBatch(GraphicsDevice);
		InitTiledMap();
		InitContentReloader();
	}


	//dictionary between object and custom class. this probably should be in another file.
	Dictionary<DotTiled.Object, GameClasses.FilledShape> FilledShapes = new();


	enum MovePattern {
		BackAndForth,
		Clockwise,
		Counterclockwise
	}
	enum FaceDirection {
		Up, Down, Left, Right
	}

	enum Chirality {
		CounterClockwise,
		Clockwise
	}
	enum SpawnType {
		Demonhead,
		Sportsman,
		Ghost,
		Ricochet,
		Comet,
		Goblin,
		Salamander
	}
	enum KeyType {
		Local,
		Master
	}
	enum DoorType {
		Local,
		Master
	}
	enum CardinalDirection {
		NE, SE, SW, NW
	}

	//[Flags]
	//public enum Direction {
	//	Up, Down, Left, Right
	//}


	void InitTiledMap() {
		//Add all un-implemented Tiled classes here:
		//https://github.com/dcronqvist/DotTiled/issues/42
		//string[] custom_classes = [
		//	"Goblin",
		//	"Unicorn",
		//	"Stone",
		//	"Wall",
		//	"Walling",
		//	"Spawn",
		//	"Item",
		//	"Clay",
		//	"ClayCracked",
		//	"Other",
		//	"Extra",
		//	"DoorOpen",
		//	"DoorClosed",
		//	"Key",
		//	"Actor",
		//	"Spawner",
		//	"Emblem",

		//];
		List <ICustomTypeDefinition> customTypeDefinitions = new();
		//foreach (var c in custom_classes) {
		//	customTypeDefinitions.Add(new CustomClassDefinition { Name = c });
		//}

		//And the properly implemented classes & enums here:
		//Note: at present, custom enums must be implemented.
		//https://dcronqvist.github.io/DotTiled/docs/essentials/custom-properties.html#custom-types
		//customTypeDefinitions.Add(CustomClassDefinition.FromClass<CustomTypes.FilledShape>());
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<CustomTypes.Direction>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<FaceDirection>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<MovePattern>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<Chirality>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<CardinalDirection>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<DoorType>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<KeyType>(CustomEnumStorageType.String));
		//customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<SpawnType>(CustomEnumStorageType.String));

		//tiledMap = new TiledMap(graphics.GraphicsDevice, "Content/LesserKeysLevels", "namedLevels/Ad Hoc.tmx");
		tiledMap = new TiledMap(graphics.GraphicsDevice, "Content/tiled", "map.tmx", customTypeDefinitions);
		//tiledMap = new TiledMap(graphics.GraphicsDevice, "Content/tiled", "map.tmx");
		TiledMap.DrawGrid = true;
		
		foreach(var obj in tiledMap.AllObjects) {
			//Parse out custom types
			//Console.WriteLine($"{obj.Name}:");
			foreach (var prop in obj.Properties) {
				
				//Console.WriteLine($"{prop.Name}: {prop.ValueString}" );
			}
			switch (obj.Type) {
				case "FilledShape": {
					var shape = obj.MapPropertiesTo<CustomTypes.FilledShape>();
					FilledShapes.Add(obj, new GameClasses.FilledShape(shape));
					Console.WriteLine($"{obj.Type}, {obj.X}, {obj.Y}");
				}
				break;
			}
		}
	}


	void InitContentReloader() {
#if (DEBUG)
		var solution_dir = "../../../";
		contentReloader = new ContentReloader(Path.Combine(solution_dir, tiledMap.TiledProjectDirectory), tiledMap.TiledProjectDirectory);
#else
		//If auto-reload for release is not desired, comment this out. R Key reloads map from file system regardless.
		contentReloader = new ContentReloader(tiledMap.TiledProjectDirectory, tiledMap.TiledProjectDirectory);
#endif
	}


	public void ReloadMap() {
		string path = tiledMap.TiledProjectDirectory;
		string file = tiledMap.MapFilePath;
		var typeDefinitions = tiledMap.CustomTypeDefinitions;
		tiledMap = new TiledMap(graphics.GraphicsDevice, path, file, typeDefinitions);
	}

	public Rectangle viewport_bounds => new Rectangle(0,0,graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

	bool reset_held;
	bool leftclick_held;
	Point click_pos;
	Point view_pos;

	int milliseconds = 0;
	Random rnd = new Random();

	protected override void Update(GameTime gameTime) {

		var keyboard = Keyboard.GetState();
		var mouse = Mouse.GetState();
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
			Exit();


		bool reset_prev = reset_held;
		reset_held = keyboard.IsKeyDown(Keys.R);
		bool reset_pressed = reset_held && !reset_prev;
		if (reset_pressed || contentReloader.ReloadFlag) {
			ReloadMap();
			contentReloader.ReloadFlag = false;
		}

		bool leftclick_prev = leftclick_held;
		leftclick_held = mouse.LeftButton == ButtonState.Pressed;
		bool leftclick_pressed = leftclick_held && !leftclick_prev;

		if (leftclick_pressed) {
			click_pos = mouse.Position - view_pos;
		}
		if (IsActive && leftclick_held) {
			view_pos = mouse.Position - click_pos;
		}


		int prev_milliseconds = milliseconds;
		milliseconds += gameTime.ElapsedGameTime.Milliseconds;

		if (milliseconds != prev_milliseconds && milliseconds % 20 == 0) {
			milliseconds = 0;
			Point point = new(0, 0);

			//TryGetValue for uncertainty.
			//if you are certain layer exists:
			//TileLayer orangestuff = TileLayersByName["OrangeStuff"]
			if (tiledMap.TileLayersByName.TryGetValue("OrangeStuff", out TileLayer orangestuff)) {
				var gid = tiledMap.GetTileGID(orangestuff, point);
				var (tileset, id) = tiledMap.GetTileID(orangestuff, point);
				tiledMap.SetTileByID(orangestuff, point, tileset, id + 1);
				tiledMap.SetTileByID(orangestuff, new(6, 0), tileset, id + 5);
				tiledMap.SetTileByID(orangestuff, new(0, 6), tileset, id + 9);
				tiledMap.SetTileByID(orangestuff, new(6, 6), tileset, id + 13);

				point = new(rnd.Next(7), rnd.Next(7));
				id = (uint)rnd.Next((int)tileset.TileCount);
				tiledMap.SetTileByID(orangestuff, point, tileset, id);
			}

		}


		base.Update(gameTime);
	}




	protected override void Draw(GameTime gameTime) {
		GraphicsDevice.Clear(tiledMap.BackgroundColor);
		spriteBatch.Begin(samplerState: SamplerState.PointWrap); //Wrap for image layers with repeat-x or repeat-y
		tiledMap.Draw(spriteBatch, view_pos, viewport_bounds);

		//var shapeslayer = tiledMap.ObjectLayersByName["Shapes"];
		//var objects = shapeslayer.Objects;
		//foreach (var obj in objects) {

		//	Primitives2D.DrawRectangle(spriteBatch, new Rectangle((int)obj.X, (int)obj.Y, 300, 300), Color.Azure);

		//	if (obj.Type == "FilledShape") { //there is probably better way to do this
		//		FilledShapes[obj].Draw(spriteBatch, obj, new Point(0, 0));
		//		continue;
		//	}
		//}
		//Primitives2D.DrawRectangle(spriteBatch, new Rectangle(30, 30, 300, 300), Color.Azure);
		spriteBatch.End();

		spriteBatch.Begin(samplerState: SamplerState.PointWrap); //Wrap for image layers with repeat-x or repeat-y

		int i = 0;
		foreach (var (obj, shape) in FilledShapes) {
			shape.Draw(spriteBatch, obj, view_pos);
			//Console.WriteLine("shape");
			//Rectangle bounds = TiledMap.GetObjectBounds(obj);

			//Console.WriteLine(bounds);
			//Primitives2D.FillRectangle(spriteBatch, new Rectangle(i * 10, i * 10, 100, 100), Color.Azure);
			i++;
		}


		spriteBatch.End();
		base.Draw(gameTime);
	}
}