using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class DarkMirror : ModItem
    {

        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<OrangeRed>();
            Item.width = 38;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 120;
            Item.useTime = 120;
            Item.consumable = false;
            Item.value = -1;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.MagicMirror);
            recipe.AddIngredient(ModContent.ItemType<BewitchedTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 15);
            recipe.AddIngredient(ItemID.SoulofNight, 8);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.RemixMap)
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorAdventure", LangUtils.GetTextValue("Items.DarkMirror.RemixMode")));
            }
            else if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !tsorcRevampWorld.RemixMap)
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorAdventure", LangUtils.GetTextValue("Items.DarkMirror.AdvMode")));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorDefault", LangUtils.GetTextValue("Items.DarkMirror.NonAdv")));
            }
        }

        public override bool? UseItem(Player player)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>());
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.DarkMirror.Summon"), Color.Blue);
                return true;
            }
            else
            {

                for (int i = 0; i < 30; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(10, 0).RotatedBy(offset.ToRotation());
                    Dust.NewDustPerfect(player.Center + offset, DustID.ShadowbeamStaff, velocity).noGravity = true;
                }
                return false;
            }
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 4;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Gray * 0.3f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 4;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Gray * 0.3f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }

        double timeRate;
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (player.itemTime == 0)
            {
                if (!Main.dayTime)
                {
                    timeRate = 0;
                }
                else
                {
                    timeRate = 2 * (54000 - Main.time) / (Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item));
                }
            }

            if (player.itemTime > (Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 2)
            {
                Main.time += timeRate;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (player.itemTime == 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.DarkMirror.Engulf"), Color.Blue);
                    player.itemTime = (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item));
                }
                else if (player.itemTime == (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 4)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60);

                    //destroy grapples
                    player.grappling[0] = -1;
                    player.grapCount = 0;
                    for (int p = 0; p < 1000; p++)
                    {
                        if (Main.projectile[p].active && Main.projectile[p].owner == player.whoAmI && Main.projectile[p].aiStyle == 7)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    if (tsorcRevampWorld.RemixMap)
                    {
                        player.position = new Vector2(6148, 1751) * 16;
                    }
                    else
                    {
                        //Legacy 2000-space; +200 band confirmed (neighboring sign 5774,1781 -> +200).
                        player.position = ExpandedWorldTransform.MapWorld(new Vector2(5760, 1774) * 16);
                    }
                    


                    player.gravDir = 1;
                    player.velocity.X = 0f;
                    player.velocity.Y = 0f;
                    player.fallStart = (int)player.Center.Y;

                    for (int i = 0; i < 70; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                        Vector2 velocity = new Vector2(10, 0).RotatedBy(offset.ToRotation());
                        Dust.NewDustPerfect(player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                        Dust.NewDustPerfect(player.Center, DustID.ShadowbeamStaff, velocity / 2).noGravity = true;
                    }

                }
                else if (player.itemTime >= (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 4)
                {
                    for (int i = 0; i < 35; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                        Vector2 velocity = new Vector2(-5, 0).RotatedBy(offset.ToRotation());
                        Dust.NewDustPerfect(player.Center + offset, DustID.ShadowbeamStaff, velocity).noGravity = true;
                    }
                }
                else
                {
                    for (int i = 0; i < 35; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                        Vector2 velocity = new Vector2(10, 0).RotatedBy(offset.ToRotation());
                        Dust.NewDustPerfect(player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                    }
                }
            }
        }
    }
}
