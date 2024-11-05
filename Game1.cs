﻿using DotTiled;
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
		graphics.PreferredBackBufferWidth = 1024;
		graphics.PreferredBackBufferHeight = 768;
		graphics.ApplyChanges();
		base.Initialize();
	}

	protected override void LoadContent() {
		spriteBatch = new SpriteBatch(GraphicsDevice);
		InitTiledMap();
		InitContentReloader();
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

		tiledMap = new(graphics.GraphicsDevice, "Content/tiled", "map.tmx", customTypeDefinitions);
	}


	void InitContentReloader() {
#if (DEBUG)
		var solution_dir = "../../../";
		contentReloader = new(Path.Combine(solution_dir, tiledMap.TiledProjectDirectory), tiledMap.TiledProjectDirectory);
#else
		//If auto-reload for release is not desired, comment this out. R Key reloads map from file system regardless.
		contentReloader = new(tiledMap.TiledProjectDirectory, tiledMap.TiledProjectDirectory);
#endif
	}


	public void ReloadMap() {
		string path = tiledMap.TiledProjectDirectory;
		string file = tiledMap.MapFilePath;
		var typeDefinitions = tiledMap.CustomTypeDefinitions;
		tiledMap = new(graphics.GraphicsDevice, path, file, typeDefinitions);
	}

	public Rectangle viewport_bounds => new Rectangle(0,0,graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

	bool reset_held;
	bool leftclick_held;
	Point click_pos;
	Point view_pos;

	int milliseconds = 0;
	Random rnd = new Random();

	protected override void Update(GameTime gameTime) {
		int prev_milliseconds = milliseconds;
		milliseconds = gameTime.TotalGameTime.Milliseconds;

		if (milliseconds != prev_milliseconds && milliseconds % 5 == 0) {
			Point point = new(0, 0);
			TileLayer orange = tiledMap.TileLayersByName["OrangeStuff"];
			var gid = tiledMap.GetTileGID(orange, point);
			var (tileset, id) = tiledMap.GetTileID(orange, point);
			tiledMap.SetTileByID(orange, point, tileset, id + 1);
			tiledMap.SetTileByID(orange, new(6, 0), tileset, id + 4);
			tiledMap.SetTileByID(orange, new(0, 6), tileset, id + 8);
			tiledMap.SetTileByID(orange, new(6, 6), tileset, id + 12);

			point = new(rnd.Next(7), rnd.Next(7));
			id = (uint)rnd.Next((int)tileset.TileCount);
			tiledMap.SetTileByID(orange,  point, tileset, id);
		}

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