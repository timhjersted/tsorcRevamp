using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Debug
{
    public class DebugTome : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.damage = 999999;
            Item.knockBack = 4;
            Item.crit = 4;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item11;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.value = 10000;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 24f;
            Item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {

            Main.NewText(player.Center / 16);

            //Main.NewText(Main.tile[(player.Center / 16).ToPoint()].WallType);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, player.Center);



            //NPC.NewNPC(source, (int)position.X, (int)position.Y, ModContent.NPCType<NPCs.Special.AbyssCataclysm>());

            //NPCs.Bosses.PrimeV2.PrimeV2.ActuatePrimeArena();

            //NPC.NewNPC(Item.GetSource_FromThis(), (int)Main.MouseWorld.X, (int)Main.MouseWorld.Y, ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>());
            //Projectile.NewProjectileDirect(source, position, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.GlowingEnergy>(), 0, 0, Main.myPlayer, 600, UsefulFunctions.ColorToFloat(new Color(1.0f, 0.4f, 0.1f, 1.0f)));

            //Uncomment this to make the debug tome max out your perma potions
            //return false;

            //return false;

            /*
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            foreach(KeyValuePair<int, int> k in modPlayer.consumedPotions)
            {
                modPlayer.consumedPotions[k.Key] = 999;
            }*/
            return false;
        }

        //For multiplayer testing, because I only have enough hands for one keyboard. Makes the player holding it float vaguely near the next other player.
        public override void UpdateInventory(Player player)
        {
            if (player.name == "MPTestDummy")
            {
                if (player.whoAmI == 0)
                {
                    if (Main.player[1] != null && Main.player[1].active)
                    {
                        player.position = Main.player[1].position;
                        player.position.X += 300;
                        player.position.Y += 300;
                    }
                }
                else
                {
                    if (Main.player[0] != null && Main.player[0].active)
                    {
                        player.position = Main.player[0].position;
                        player.position.X += 300;
                        player.position.Y += 300;
                    }
                }
            }
        }

        public override bool CanUseItem(Player player)
        {
            //if (player.name == "Zeodexic" || player.name.Contains("Sam") || player.name == "Chroma TSORC test")
            {
                return true;
            }
            //return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            // player is 2x3 size in terraria, so player.position is actually the postion of top left block
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "CoordinateTooltip", "Player World Coordinate: " + player.position.X + ", "+ player.position.Y));
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "CoordinateTooltip", "T-edit Coordinate: " + (int)(player.position.X / 16) + ", "+ (int)(player.position.Y / 16)));
        }
    }
}