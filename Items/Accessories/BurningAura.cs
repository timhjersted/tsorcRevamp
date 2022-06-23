using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class BurningAura : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("An array of meteorite shards, floating in a slow orbit" +
                                "\nPassively launches homing fireballs at nearby enemies" +
                                "\nFireballs scale in power with each boss you kill" +
                                "\nAlso increases damage dealt to burning foes by 5%"); ;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "killcount", $"Current count: {((tsorcRevampWorld.Slain == null) ? 0 : tsorcRevampWorld.Slain.Count)}"));
            base.ModifyTooltips(tooltips);
        }
        public override void UpdateEquip(Player player)
        {
            for (int j = 0; j < 5; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(45, 45);
                Vector2 dustPos = player.Center + dir;
                Dust.NewDustPerfect(dustPos, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;
            }

            tsorcRevampPlayer ModPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            ModPlayer.WeakeningBurn = true;

            if (Main.GameUpdateCount % 45 == 0)
            {
                int? target = UsefulFunctions.GetClosestEnemyNPC(player.Center);
                if (target != null && Main.npc[target.Value].Distance(player.Center) < 1000)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(45, 45);
                    Vector2 velocity = UsefulFunctions.GenerateTargetingVector(player.Center + offset, Main.npc[target.Value].Center, 10);
                    int damage = (int)(tsorcRevampWorld.Slain.Count * 3f);
                    if (tsorcRevampWorld.SuperHardMode)
                    {
                        damage *= 2;
                    }
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center + offset, velocity, ModContent.ProjectileType<Projectiles.HomingFireball>(), damage, 0.5f, player.whoAmI);
                }
            }
        }

        public override void UpdateVanity(Player player)
        {
            for (int j = 0; j < 5; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(45, 45);
                Vector2 dustPos = player.Center + dir;
                Dust.NewDustPerfect(dustPos, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;
            }
        }

    }
}
