﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace NuTiled;
public class Game1 : Game {
	private GraphicsDeviceManager graphics;
	private SpriteBatch spriteBatch;


	public Game1() {
		graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;

	}

	//public float Zoom { get; } = 2;

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
		tiledMap = new(Content, "tiled", "map.tmx");
		InitContentReloader();
	}


	void InitContentReloader() {
#if (DEBUG || TEST)
		var solution_content_dir = "../../../Content/";
		contentReloader = new(Path.Combine(solution_content_dir, tiledMap.ContentDirectory), Path.Combine(Content.RootDirectory, tiledMap.ContentDirectory));
#else
		//If auto-reload is not desired, comment this out. R Key reloads map from file system regardless.
		contentReloader = new(Path.Combine(Content.RootDirectory, tiledMap.ContentDirectory), Path.Combine(Content.RootDirectory, tiledMap.ContentDirectory));
#endif
	}


	public void ReloadMap() {
		string path = tiledMap.ContentDirectory;
		string file = tiledMap.MapFile;
		tiledMap = new(Content, path, file);
		TiledMap.ReloadFlag = false;
	}


	public Rectangle viewport_bounds => new Rectangle(0,0,graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

	bool reset;
	bool leftbutton;

	Point click_pos;
	Point view_pos;

	protected override void Update(GameTime gameTime) {
		var keyboard = Keyboard.GetState();
		var mouse = Mouse.GetState();
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
			Exit();

		bool reset_prev = reset;
		reset = keyboard.IsKeyDown(Keys.R);
		if ((reset && !reset_prev) || TiledMap.ReloadFlag) {
			ReloadMap();
		}

		bool leftbutton_prev = leftbutton;
		leftbutton = mouse.LeftButton == ButtonState.Pressed;
		bool leftclickpressed = leftbutton && !leftbutton_prev;
		if (leftclickpressed) {
			click_pos = mouse.Position - view_pos;
		}
		if (IsActive && leftbutton) {
			view_pos = mouse.Position - click_pos;
		}

		base.Update(gameTime);
	}




	protected override void Draw(GameTime gameTime) {
		GraphicsDevice.Clear(tiledMap.BackgroundColor);
		spriteBatch.Begin(samplerState: SamplerState.PointWrap); //PointWrap for pixelatted scaling and image layers with repeat-x or repeat-y
		{
			tiledMap.Draw(spriteBatch, view_pos, viewport_bounds);
		}
		spriteBatch.End();
		base.Draw(gameTime);
	}
}