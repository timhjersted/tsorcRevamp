using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.SunsetQuasar {
    internal class SunsetQuasarToken : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";
        public override void SetStaticDefaults() {
            // DisplayName.SetDefault("Sunset Quasar");
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
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.SunsetQuasarBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.SunsetQuasarBuff>())) {
                Projectile.timeLeft = 2;
            }

            Projectile.Center = player.Center;
        }

        public override bool? CanDamage() {
            return false;
        }
    }
}
