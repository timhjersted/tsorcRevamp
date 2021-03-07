using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;




namespace tsorcRevamp.Tiles
{
	public class SoulSkullNew : ModTile
	{
		public override void SetDefaults()
		{
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.addTile(Type);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			ModTranslation name = CreateMapEntryName();
			name.SetDefault("Soul Skull");
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
			if (tile.frameX == 0)
			{
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
					if (Main.rand.NextBool(2) && frameX == 0)
					{
						int style = frameY / 2;
						//if (frameY / 18 % 3 == 0)
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
								int dust = Dust.NewDust(new Vector2(i * 16 + 16, j * 16 + 6), 4, 4, dustChoice, 0f, 0f, 100, default(Color), 1f);
								//if (Main.rand.Next(3) != 0)
								{
									Main.dust[dust].noGravity = true;
								}
								Main.dust[dust].velocity *= 0.3f;
								Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 0.5f;

							}
						}
					}

					if (Main.rand.NextBool(2) && frameX == 0)
					{
						int style = frameY / 2;
						//if (frameY / 18 % 3 == 0)
						if (frameY / 18 % 2 == 0)
						{
							int dustChoice = -1;
							if (style == 0)
							{
								dustChoice = 89; // A purple dust.

							}
							// We can support different dust for different styles here
							if (dustChoice != -1)
							{
								int dust = Dust.NewDust(new Vector2(i * 16 + 7, j * 16 + 19), 4, 4, dustChoice, 0f, 0f, 100, default(Color), .7f); //left eye
								Main.dust[dust].noGravity = true;
								Main.dust[dust].velocity *= 0.1f;
								Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 0.1f;
							}
						}
					}

					if (Main.rand.NextBool(2) && frameX == 0)
					{
						int style = frameY / 2;
						//if (frameY / 18 % 3 == 0)
						if (frameY / 18 % 2 == 0)
						{
							int dustChoice = -1;
							if (style == 0)
							{
								dustChoice = 89; // A purple dust.

							}
							// We can support different dust for different styles here
							if (dustChoice != -1)
							{
								int dust = Dust.NewDust(new Vector2(i * 16 + 12, j * 16 + 19), 4, 4, dustChoice, 0f, 0f, 100, default(Color), .7f); //right eye
								Main.dust[dust].noGravity = true;
								Main.dust[dust].velocity *= 0.1f;
								Main.dust[dust].velocity.Y = Main.dust[dust].velocity.Y - 0.1f;

							}
						}
					}
				}
		}
		/*public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height)
		{
			offsetY = 0;
			if (WorldGen.SolidTile(i, j - 1))
			{
				offsetY = 2;
				if (WorldGen.SolidTile(i - 1, j + 1) || WorldGen.SolidTile(i + 1, j + 1))
				{
					offsetY = 4;
				}
			}
		}*/

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX == 0)
			{
				//Tile tile = Main.tile[i, j];
				Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
				if (Main.drawToScreen)
				{
					zero = Vector2.Zero;
				}
				int height = tile.frameY == 36 ? 18 : 16;
				Main.spriteBatch.Draw(mod.GetTexture("Tiles/SoulSkull_Glow"), new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.frameX, tile.frameY, 16, height), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			}

			/*SpriteEffects effects = SpriteEffects.None;
			if (i % 2 == 1)
			{
				effects = SpriteEffects.FlipHorizontally;
			}*/

			/*Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
			if (Main.drawToScreen)
			{
				zero = Vector2.Zero;
			}

			Tile tile = Main.tile[i, j];
			int width = 34;
			int offsetY = 0;
			int height = 34;
			TileLoader.SetDrawPositions(i, j, ref width, ref offsetY, ref height);
			//var flameTexture = mod.GetTexture("Tiles/SoulSkull_Glow"); // We could also reuse Main.FlameTexture[] textures, but using our own texture is nice.
			Main.spriteBatch.Draw(mod.GetTexture("Tiles/SoulSkull_Glow"), new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.frameX, tile.frameY, 16, height), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			//ulong num190 = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (long)(uint)i);*/
			// We can support different flames for different styles here: int style = Main.tile[j, i].frameY / 54;
			/*for (int c = 0; c < 7; c++)
			{
				float shakeX = Utils.RandomInt(ref num190, -10, 11) * 0.15f;
				float shakeY = Utils.RandomInt(ref num190, -10, 1) * 0.35f;
				Main.spriteBatch.Draw(flameTexture, new Vector2(i * 16 - (int)Main.screenPosition.X - (width - 16f) / 2f + shakeX, j * 16 - (int)Main.screenPosition.Y + offsetY + shakeY) + zero, new Rectangle(tile.frameX, tile.frameY, width, height), new Color(100, 100, 100, 0), 0f, default(Vector2), 1f, 0f);
			}*/
		}
	
		public override void MouseOver(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX == 0)
			{
				Player player = Main.LocalPlayer;
				player.noThrow = 2;
				player.showItemIcon = true;
				player.showItemIcon2 = ModContent.ItemType<SoulSkullItem>();
			}
		}
		public override void MouseOverFar(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX == 0)
			{
				MouseOver(i, j);
				Player player = Main.LocalPlayer;
				if (player.showItemIconText == "")
				{
					player.showItemIcon = true;
					player.showItemIcon2 = ModContent.ItemType<SoulSkullItem>();
				}
			}
		}

		public override bool NewRightClick(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX == 0)
			{
				Main.PlaySound(SoundID.NPCDeath52.WithVolume(.35f).WithPitchVariance(.3f)); // Plays sound.
				Item.NewItem(new Vector2(i * 16, j * 16), 16, 16, mod.ItemType("FadingSoul"), 1);
				tile.frameX = 36;
			}
			//Main.PlaySound(SoundID.NPCDeath52.WithVolume(.35f).WithPitchVariance(.3f)); // Plays sound.

			return false;

		}

	}

	public class SoulSkullItemNew : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Soul Skull New");
		}

		public override void SetDefaults()
		{
			item.CloneDefaults(ItemID.ArmorStatue);
			item.createTile = ModContent.TileType<SoulSkullNew>();
			item.placeStyle = 0;
		}
	}
}