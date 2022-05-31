using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DarkMirror : ModItem {

        public override void SetDefaults() {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 38;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 120;
            Item.useTime = 120;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.value = -1;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {               
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorAdventure",
                "You look into the mirror and see your reflection looking back at you... \n" +
                "As you continue to gaze into the mirror, the background behind \n" +
                "your reflection comes into focus, revealing a dark pyramid... \n" +
                "Use the mirror at night to continue looking into the eyes of your reflection...  \n" +
                "Or throw it away and rid yourself of this dark relic..."));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "DarkMirrorDefault", 
                "You look into the mirror and see your reflection looking back at you... \n" +
                "As you continue to gaze into the mirror, the background behind \n" +
                "your reflection becomes murky, as if peering into a dark abyss... \n" +
                "Use the mirror to continue looking into the eyes of your reflection...  \n" +
                "Or throw it away and rid yourself of this dark relic..."));
            }
        }

        public override bool? UseItem(Player player) {
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

        double timeRate;
        public override void UseStyle(Player player)
        {
            if(player.itemTime == 0)
            {
                if (!Main.dayTime)
                {
                    timeRate = 0;
                }
                else
                {
                    timeRate = 2 * (54000 - Main.time) / (Item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, Item));
                }
            }

            if(player.itemTime > (Item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, Item)) / 2)
            {
                Main.time += timeRate;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (player.itemTime == 0)
                {
                    Main.NewText("The mirror's shadow engulfs you...", Color.Blue);
                    player.itemTime = (int)(Item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, Item));
                }
                else if (player.itemTime == (int)(Item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, Item)) / 4)
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
                else if (player.itemTime >= (int)(Item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, Item)) / 4)
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
