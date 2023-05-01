using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class BurningAura : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.expert = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "killcount", $"Current count: {((tsorcRevampWorld.NewSlain == null) ? 0 : tsorcRevampWorld.NewSlain.Count)}"));
            base.ModifyTooltips(tooltips);
        }
        public override void UpdateEquip(Player player)
        {
            if (Main.netMode != NetmodeID.Server && player == Main.LocalPlayer)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.BurningShards>()] < 5 && Main.GameUpdateCount % 15 == 0)
                {
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.BurningShards>(), 0, 0, player.whoAmI);
                }
            }
            Vector2 dir = Main.rand.NextVector2CircularEdge(60, 60);
            Vector2 dustPos = player.Center + dir;
            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;

            tsorcRevampPlayer ModPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            ModPlayer.BurningAura = true;

            
        }

        public override void UpdateVanity(Player player)
        {
            Vector2 dir = Main.rand.NextVector2CircularEdge(60, 60);
            Vector2 dustPos = player.Center + dir;
            Dust.NewDustPerfect(dustPos, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;
        }

    }
}
