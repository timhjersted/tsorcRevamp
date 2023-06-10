using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
    public class OrbOfSpiritualityDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (Main.GameUpdateCount % 1 == 0)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SpiritRushTimer -= 0.0167f;
                player.GetModPlayer<tsorcRevampPlayer>().DashCD -= 0.0167f;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().DashCD > 0f)
            {
                player.immune = true;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SpiritRushTimer > 0f)
            {
                player.immune = true;
                player.velocity = UsefulFunctions.Aim(player.Center, Main.MouseWorld, 15f);
                if (Main.GameUpdateCount % 3 == 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.One, ModContent.ProjectileType<OrbOfSpiritualityFlameNoMana>(), player.HeldItem.damage, player.HeldItem.knockBack, Main.myPlayer);
                }
            }
            if (player.buffTime[buffIndex] == 1)
            {
                player.GetModPlayer<tsorcRevampPlayer>().Dashes = 3;
            }
        }
    }
}
