using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
    public class OrbOfSpiritualityDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SpiritRushTimer -= 0.0167f;
            player.GetModPlayer<tsorcRevampPlayer>().SpiritRushCooldown -= 0.0167f;
            if (player.GetModPlayer<tsorcRevampPlayer>().SpiritRushCooldown > 0f)
            {
                player.immune = true;
            }
            if (player.GetModPlayer<tsorcRevampPlayer>().SpiritRushTimer > 0f)
            {
                player.immune = true;
                if (Main.GameUpdateCount % 3 == 0)
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.One, ModContent.ProjectileType<OrbOfSpiritualityFlameNoMana>(), player.HeldItem.damage, player.HeldItem.knockBack, player.whoAmI);
                }
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SpiritRushVisual>()] == 0)
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<SpiritRushVisual>(), 0, 0, player.whoAmI);
            }
            if (player.buffTime[buffIndex] == 1)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SpiritRushCharges = 3;
                player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>(), OrbOfSpirituality.DashCD * 60);
            }
        }
    }
}
