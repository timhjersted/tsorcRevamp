using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Steamworks;

namespace tsorcRevamp.Tiles
{
	[Autoload(false)]
	public class Bonfire : ModTile
	{
        public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 18 };
			animationFrameHeight = 56;
			TileObjectData.addTile(Type);
			ModTranslation name = CreateMapEntryName();
			name.SetDefault("Bonfire");
			AddMapEntry(new Color(215, 60, 0), name);
			dustType = 30;
			disableSmartCursor = true;
			Main.tileLighted[Type] = true;
			Main.tileFrameImportant[Type] = true;
			Main.tileBlockLight[Type] = false;
			Main.tileNoAttach[Type] = true;
			Main.tileWaterDeath[Type] = false;
			Main.tileLavaDeath[Type] = false;

		}

		private readonly int animationFrameWidth = 54;

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.5f;
			g = 0.35f;
			b = 0.1f;
		}
		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			Item.NewItem(i * 16, j * 16, 16, 32, ModContent.ItemType<BonfireItem>());
		}

		public override void AnimateTile(ref int frame, ref int frameCounter)
		{
			// Spend 8 ticks on each of 9 frames
			frameCounter++;
			if (frameCounter > 8)
			{
				frameCounter = 0;
				frame--;			//Yep, animated in reverse!
				if (frame < 1)		//Miss first frame of animation - 'bonfire unlit' frame
				{
					frame = 8; 
				}
			}
		}

		public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			Texture2D texture;
			if (Main.canDrawColorTile(i, j))
			{
				texture = Main.tileAltTexture[Type, (int)tile.TileColor];
			}
			else
			{
				texture = Main.tileTexture[Type];
			}
			Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
			if (Main.drawToScreen)
			{
				zero = Vector2.Zero;
			}
			int width = tile.TileFrameX % animationFrameWidth == 54 ? 18 : 16;
			int height = tile.TileFrameY % animationFrameHeight == 36 ? 18 : 16; //This seems to say that of the 3 Y tiles, the bottom one is 18px tall, the other two 16px. 
			int animate = 0;
			if (tile.TileFrameX >= 0) //change to 54 once right-click to light is implemented
			{
				animate = Main.tileFrame[Type] * animationFrameWidth;
			}
			Main.spriteBatch.Draw(texture, new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.TileFrameX + animate, tile.TileFrameY, width, height), Lighting.GetColor(i, j), 0f, default, 1f, SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(Mod.GetTexture("Tiles/Bonfire_Glow"), new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.TileFrameX + animate, tile.TileFrameY, width, height), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			Dust.NewDust(zero, 4, 4, 6, 0, 0, 100, default, 1f);
			return false;
		}
	}

	[Autoload(false)]
	public class BonfireItem : ModItem
	{
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dark Souls Bonfire");
			Tooltip.SetDefault("Right-click to light" + //re-do once finalized
			"\nOnce lit, right-clicking bottom 2 tiles sets spawn" +
			"\nRight-clicking upper tiles opens personal storage");
		}

		public override void SetDefaults()
		{
			Item.CloneDefaults(ItemID.ArmorStatue);
			Item.createTile = ModContent.TileType<Bonfire>();
			Item.placeStyle = 0;
		}
	}
}