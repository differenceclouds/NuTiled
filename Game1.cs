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

	public Rectangle viewport_bounds => new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

	TiledMap tiledMap;

	protected override void LoadContent() {
		spriteBatch = new SpriteBatch(GraphicsDevice);
		tiledMap = new(Content, "tiled", "map.tmx");
	}

	protected override void Update(GameTime gameTime) {
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			Exit();

		// TODO: Add your update logic here

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime) {
		GraphicsDevice.Clear(tiledMap.BackgroundColor);
		spriteBatch.Begin(samplerState: SamplerState.PointWrap);

		tiledMap.Draw(spriteBatch, viewport_bounds);

		spriteBatch.End();
		base.Draw(gameTime);
	}
}