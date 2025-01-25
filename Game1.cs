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
	GraphicsDeviceManager graphics;
	SpriteBatch spriteBatch;


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

	/// <summary>
	/// Get full path of input relative to the executing directory (necessary for macos)
	/// </summary>
	public static string GetExecutingDir(string relativePath) {
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		return Path.Combine(baseDirectory, relativePath);
	}
	
	
	void InitTiledMap() {
		//Add all un-implemented Tiled classes here:
		//https://github.com/dcronqvist/DotTiled/issues/42
		string[] custom_classes = [
			"Goblin",
		];
		List <ICustomTypeDefinition> customTypeDefinitions = new();
		foreach (var c in custom_classes) {
			customTypeDefinitions.Add(new CustomClassDefinition { Name = c });
		}

		//And the properly implemented classes & enums here:
		//Note: at present, custom enums must be implemented.
		//https://dcronqvist.github.io/DotTiled/docs/essentials/custom-properties.html#custom-types
		customTypeDefinitions.Add(CustomClassDefinition.FromClass<CustomTypes.FilledShape>());
		customTypeDefinitions.Add(CustomEnumDefinition.FromEnum<CustomTypes.Direction>());
		string projectDir = GetExecutingDir("Content/tiled");
		tiledMap = new TiledMap(graphics.GraphicsDevice, projectDir, "map.tmx", customTypeDefinitions);
		TiledMap.DrawGrid = true;
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

	Rectangle viewport_bounds => new Rectangle(0,0,graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

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
		spriteBatch.End();
		base.Draw(gameTime);
	}
}