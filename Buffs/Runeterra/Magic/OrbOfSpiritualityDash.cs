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

        float cooldownToRefund = 0;
        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SpiritRushTimer -= 0.0167f;
            modPlayer.SpiritRushCooldown -= 0.0167f;

            if (modPlayer.SpiritRushCooldown > 0f)
            {
                player.immune = true;

                if (player.buffTime[buffIndex] < 10) // dont expire during dash
                    player.buffTime[buffIndex]++;
            }
            else
            {
                cooldownToRefund += 0.75f; // Refund part of the cooldown spent not dashing
            }

            if (modPlayer.SpiritRushTimer > 0f)
            {
                player.immune = true;
                if (Main.GameUpdateCount % 3 == 0 && Main.myPlayer == player.whoAmI)
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.One, ModContent.ProjectileType<FlameOrbOfSpirituality>(), player.HeldItem.damage, player.HeldItem.knockBack, player.whoAmI, 1);
                }
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SpiritRushVisual>()] == 0 && Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<SpiritRushVisual>(), 0, 0, player.whoAmI);
            }

            if (player.buffTime[buffIndex] == 1 || (modPlayer.SpiritRushCharges == 0 && modPlayer.SpiritRushTimer <= 0f))
            {
                // Refund dash CD for each dash cooldown the player has left
                // Also refund the time this buff was active without a dash being used. ( a hidden tracker for the overall cooldown )
                int dashCooldown = ((OrbOfSpirituality.DashCD * 3) - (OrbOfSpirituality.DashCD * modPlayer.SpiritRushCharges)) * 60 - (int)cooldownToRefund;
                if (dashCooldown > 0)
                    player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>(), dashCooldown);

                modPlayer.SpiritRushCharges = 3;
                modPlayer.SpiritRushCooldown = 0;
                modPlayer.SpiritRushTimer = 0;
                cooldownToRefund = 0;
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
