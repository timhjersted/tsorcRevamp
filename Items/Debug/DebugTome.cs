using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using System;
using System.Collections.Generic;

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
            for(int i = 0; i < 50; i++)
            {
                Dust.NewDustPerfect(player.Center + new Vector2(25 - i, 0), DustID.ShadowbeamStaff, Vector2.Zero);
                Dust.NewDustPerfect(player.Center + new Vector2(0, 25 - i), DustID.ShadowbeamStaff, Vector2.Zero);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, player.Center);

            Projectile.NewProjectile(source, player.Center, speed, ModContent.ProjectileType<Projectiles.VFX.Slash>(), 0, 0f, Main.myPlayer);


            //NPCs.Bosses.PrimeV2.PrimeV2.ActuatePrimeArena();

            //NPC.NewNPC(Item.GetSource_FromThis(), (int)Main.MouseWorld.X, (int)Main.MouseWorld.Y, ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>());
            //Projectile.NewProjectileDirect(player.GetSource_ItemUse(Item), Main.MouseWorld, Main.rand.NextVector2CircularEdge(1,1), ModContent.ProjectileType<Projectiles.Enemy.Marilith.SyntheticFirestorm>(), 10, 0, player.whoAmI, 700, 60);

            //Uncomment this to make the debug tome max out your perma potions
            //return false;

            return false;
            /*
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            foreach(KeyValuePair<Terraria.ModLoader.Config.ItemDefinition, int> k in modPlayer.consumedPotions)
            {
                if(k.Key.Type == ModContent.ItemType<Items.Potions.CharcoalPineResin>()){
                    modPlayer.consumedPotions[k.Key] = 15;
                }
                //modPlayer.consumedPotions[k.Key] = 999;
            }
            return false;*/
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
    }
}