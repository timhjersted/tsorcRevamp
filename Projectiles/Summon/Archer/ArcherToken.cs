using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Archer
{
    internal class ArcherToken : ModProjectile {
        public override void SetStaticDefaults() {
            // DisplayName.SetDefault("Archer");
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.alpha = 255;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active) {
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.NondescriptOwlBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.NondescriptOwlBuff>())) {
                Projectile.timeLeft = 2;
            }

            Projectile.Center = player.Center;
            //Main.NewText("token");
        }

        public override bool? CanDamage() {
            return false;
        }
    }
}
