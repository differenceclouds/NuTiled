using DotTiled;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using Color = Microsoft.Xna.Framework.Color;

namespace NuTiled;
public class Game1 : Game {
	private GraphicsDeviceManager graphics;
	private SpriteBatch spriteBatch;


	public Game1() {
		graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
	}

	protected override void Initialize() {
		graphics.PreferredBackBufferWidth = 1024;
		graphics.PreferredBackBufferHeight = 768;
		graphics.ApplyChanges();
		base.Initialize();
	}


	TiledMap tiledMap;
	ContentReloader contentReloader;

	protected override void LoadContent() {
		spriteBatch = new SpriteBatch(GraphicsDevice);
		InitTiledMap();
		InitContentReloader();
	}

	void InitTiledMap() {
		//The following is a workaround so if any objects with the Class property are found,
		//an exception isn't thrown if a matching class isn't implemented properly.
		//https://github.com/dcronqvist/DotTiled/issues/42
		string[] classes = [
			"Goblin",
			//"Shape",
		];
		List <CustomClassDefinition> customClassDefinitions = new();
		foreach (var c in classes) {
			customClassDefinitions.Add(new CustomClassDefinition { Name = c });
		}

		//And the implemented classes here:
		var shape = new CustomClassDefinition {
			Name = "Shape",
			Members = [
				new ColorProperty  { Name = "FillColor", Value = TiledMap.ColorToColor(Color.Transparent) }
			]
		};
		customClassDefinitions.Add(shape);

		tiledMap = new(graphics.GraphicsDevice, "tiled", "map.tmx", customClassDefinitions);
	}


	void InitContentReloader() {
#if (DEBUG || TEST)
		var solution_content_dir = "../../../Content/";
		contentReloader = new(Path.Combine(solution_content_dir, tiledMap.ContentDirectory), Path.Combine(Content.RootDirectory, tiledMap.ContentDirectory));
#else
		//If auto-reload for release is not desired, comment this out. R Key reloads map from file system regardless.
		contentReloader = new(Path.Combine(Content.RootDirectory, tiledMap.ContentDirectory), Path.Combine(Content.RootDirectory, tiledMap.ContentDirectory));
#endif
	}


	public void ReloadMap() {
		string path = tiledMap.ContentDirectory;
		string file = tiledMap.MapFile;
		var classDefinitions = tiledMap.CustomClassDefinitions;
		tiledMap = new(/*Content,*/ graphics.GraphicsDevice, path, file, classDefinitions);
	}


	public Rectangle viewport_bounds => new Rectangle(0,0,graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

	bool reset_held;
	bool leftclick_held;
	Point click_pos;
	Point view_pos;

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