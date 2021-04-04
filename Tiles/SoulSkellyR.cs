using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.Enums;

namespace tsorcRevamp.Tiles
{
	public class SoulSkellyR : ModTile
	{
		public override void SetDefaults()
		{
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
			TileObjectData.newTile.Height = 1;
			TileObjectData.newTile.Width = 3;
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
			TileObjectData.newTile.CoordinateHeights = new int[] { 18 };
			TileObjectData.newTile.Origin = new Point16(2, 0);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.addTile(Type);
			ModTranslation name = CreateMapEntryName();
			name.SetDefault("Soul Skelly");
			AddMapEntry(new Color(120, 250, 0), name);
			dustType = 30;
			disableSmartCursor = true;
			Main.tileLighted[Type] = true;
			Main.tileFrameImportant[Type] = true;
			Main.tileSpelunker[Type] = true;
			Main.tileBlockLight[Type] = false;
			Main.tileNoAttach[Type] = true;
			Main.tileWaterDeath[Type] = false;
			Main.tileLavaDeath[Type] = false;
		}
		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			//Item.NewItem(i * 16, j * 16, 32, 48, ModContent.ItemType<ExampleStatueItem>()); //don't want it dropping anything
		}
		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX / 54 == 0) //having this set to 0 was only indicating the first 16 pixels, aka the first horizontal block,
			{                           //hence why the 2 left blocks only had the glowmask and could be clicked
				r = 0.15f;
				g = 0.25f;
				b = 0f;
			}
		}

		public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref Color drawColor, ref int nextSpecialDrawIndex)
		{

			if (!Main.gamePaused && Main.instance.IsActive && (!Lighting.UpdateEveryFrame || Main.rand.NextBool(4)))
			{
				Tile tile = Main.tile[i, j];
				short frameX = tile.frameX;
				short frameY = tile.frameY;
				if (Main.rand.NextBool(2) && tile.frameX == 0) //If 0 is changed to 36, you end up with the dust duplicated 2 tiles past the intended location
				{
					int style = frameY / 36; //changing this doesnt seem to do anything. Not sure what it was originally. Had it on 2 for ages, but it's probably 36.
											 //if (frameY / 18 % 3 == 0) But even then makes no difference
					if (frameY / 18 % 2 == 0)
					{
						int dustChoice = -1;
						if (style == 0)
						{
							dustChoice = 89; // A green dust.

						}
						// We can support different dust for different styles here
						if (dustChoice != -1)
						{
							int dust = Dust.NewDust(new Vector2(i * 16 + 19, j * 16 - 1), 4, 4, dustChoice, 0f, 0f, 100, default(Color), 1f);
							//if (Main.rand.Next(3) != 0)
							Main.dust[dust].noGravity = true;
							Main.dust[dust].velocity *= 0.3f;
							Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 0.5f;

						}
					}
				}
			}
		}
		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX / 54 == 0)
			{
				Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
				if (Main.drawToScreen)
				{
					zero = Vector2.Zero;
				}
				int height = tile.frameY == 18 ? 18 : 16;
				Main.spriteBatch.Draw(mod.GetTexture("Tiles/SoulSkellyR_Glow"), new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.frameX, tile.frameY, 16, height), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			}
		}
		public override void MouseOver(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX / 54 == 0)
			{
				Player player = Main.LocalPlayer;
				player.noThrow = 2;
				player.showItemIcon = true;
				player.showItemIcon2 = ModContent.ItemType<SoulSkellyItemR>();
			}
		}
		public override void MouseOverFar(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX / 54 == 0)
			{
				MouseOver(i, j);
				Player player = Main.LocalPlayer;
				if (player.showItemIconText == "")
				{
					player.showItemIcon = true;
					player.showItemIcon2 = ModContent.ItemType<SoulSkellyItemR>();
				}
			}
		}
		public override bool NewRightClick(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX / 54 == 0)
			{
				Main.PlaySound(SoundID.NPCDeath52.WithVolume(.35f).WithPitchVariance(.3f)); // Plays sound.
				if (!Main.hardMode)
				{
					Item.NewItem(new Vector2(i * 16, j * 16), 16, 16, mod.ItemType("NamelessSoldierSoul"), 1);
				}
				else
				{
					Item.NewItem(new Vector2(i * 16, j * 16), 16, 16, mod.ItemType("ProudKnightSoul"), 1);
				}
				int x = i - Main.tile[i, j].frameX / 18 % 3; // 16 pixels in a block + 2 pixels for the buffer. 3 because its 3 blocks wide
				int y = j - Main.tile[i, j].frameY / 18 % 1; // 1 because it is 1 blocks tall
				for (int l = x; l < x + 3; l++)             // this chunk of code basically makes it so that when you right click one tile, 
				{              // because 3x1 tile         // it counts as the whole 2x2 tile, not 4 individual tiles that can all be clicked
					for (int m = y; m < y + 1; m++)         //Code taken from VoidMonolith - example mod
					{
						if (Main.tile[l, m] == null)
						{
							Main.tile[l, m] = new Tile();
						}
						if (Main.tile[l, m].active() && Main.tile[l, m].type == Type)
						{
							if (Main.tile[l, m].frameX < 54) //frameX because the spritesheet is horizontal
							{
								Main.tile[l, m].frameX += 54; //if spritesheet were vertical then
							}
							else
							{
								Main.tile[l, m].frameX -= 54; //frameX would have to be replaced with frameY
							}
						}
					}
				}
			}
			return false;
		}
	}
	public class SoulSkellyItemR : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Soul Skelly Right");
			Tooltip.SetDefault("Right-Click once placed to acquire a Soul of a Nameless Soldier (800 souls)" +
			"\nGives Soul of a Proud Knight (2000 souls) outside of Pre-HM" +
			"\nUsed by mapmakers for placing around the map as loot");
		}

		public override void SetDefaults()
		{
			//item.CloneDefaults(ItemID.Furnace);
			item.createTile = ModContent.TileType<SoulSkellyR>();
			item.placeStyle = 0;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.useTurn = true;
			item.useAnimation = 15;
			item.useTime = 10;
			item.autoReuse = true;
			item.maxStack = 99;
			item.consumable = true;
			item.width = 42;
			item.height = 18;
			item.value = 0;
		}
	}
}