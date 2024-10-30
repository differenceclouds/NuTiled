using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NuTiled;
public class Game1 : Game {
	private GraphicsDeviceManager graphics;
	private SpriteBatch spriteBatch;





	public Game1() {
		graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;
		graphics.PreferredBackBufferWidth = 1024;
		graphics.PreferredBackBufferHeight = 768;
	}

	protected override void Initialize() {
		base.Initialize();
	}


	TiledMap tiledMap;
	ContentReloader contentReloader;

	protected override void LoadContent() {
		contentReloader = new("../../../Content/tiled", this); //Only good for debug! 
		spriteBatch = new SpriteBatch(GraphicsDevice);
		ReloadMap();
	}


	public void ReloadMap() {
		tiledMap = new(Content, "tiled", "map.tmx");
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
		if (reset && !reset_prev) {
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
		spriteBatch.Begin(samplerState: SamplerState.PointWrap);

		tiledMap.Draw(spriteBatch, view_pos, viewport_bounds);

		spriteBatch.End();
		base.Draw(gameTime);
	}
}