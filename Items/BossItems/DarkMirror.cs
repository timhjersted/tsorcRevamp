using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DarkMirror : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.width = 38;
            item.height = 34;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 120;
            item.useTime = 120;
            item.maxStack = 1;
            item.consumable = false;
            item.value = -1;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {               
                tooltips.Add(new TooltipLine(mod, "DarkMirrorAdventure",
                "You look into the mirror and see your reflection looking back at you... \n" +
                "As you continue to gaze into the mirror, the background behind \n" +
                "your reflection comes into focus, revealing a dark pyramid... \n" +
                "Use the mirror at night to continue looking into the eyes of your reflection...  \n" +
                "Or throw it away and rid yourself of this dark relic..."));
            }
            else
            {
                tooltips.Add(new TooltipLine(mod, "DarkMirrorDefault", 
                "You look into the mirror and see your reflection looking back at you... \n" +
                "As you continue to gaze into the mirror, the background behind \n" +
                "your reflection becomes murky, as if peering into a dark abyss... \n" +
                "Use the mirror at night to continue looking into the eyes of your reflection...  \n" +
                "Or throw it away and rid yourself of this dark relic..."));
            }
        }

        public override bool UseItem(Player player) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>());
                Main.NewText("Your shadow self has manifested from your darkest fears...", Color.Blue);
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

        public override void UseStyle(Player player)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {



                if (player.itemTime == 0)
                {
                    Main.NewText("The mirror's shadow engulfs you...", Color.Blue);
                    player.itemTime = (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item));
                }
                else if (player.itemTime == (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 4)
                {
                    Main.PlaySound(SoundID.Item60);

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

                    player.position = new Vector2(5760, 1774) * 16;


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
                else if (player.itemTime >= (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 4)
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

        public override bool CanUseItem(Player player) {
            bool canUse = true;
            if ((Main.dayTime) || (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>()))) {
                
                canUse = false;
            }
            return canUse;
        }

        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(ItemID.MagicMirror, 1);
                recipe.AddIngredient(mod.GetItem("WhiteTitanite"), 10);
                recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 15);
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }
    }
}
