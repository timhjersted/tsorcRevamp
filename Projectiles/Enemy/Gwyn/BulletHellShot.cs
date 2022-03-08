using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn {
    class BulletHellShot : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Lightning Spear");
        }
        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 32;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 225;
            projectile.tileCollide = false;
            projectile.aiStyle = -1;
            projectile.light = 0.7f;
        }
        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation();
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 1500);
        }
    }
}
