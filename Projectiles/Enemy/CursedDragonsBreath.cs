using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class CursedDragonsBreath : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 6;
            projectile.height = 6;
            aiType = 85;
            projectile.alpha = 255;
            projectile.aiStyle = 23;
            projectile.damage = 80;
            projectile.timeLeft = 3600;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.tileCollide = true;
            projectile.MaxUpdates = 2;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 85;
            return true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.Bleeding, 3600);
            target.AddBuff(BuffID.Weak, 3600);
            target.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000);
        }
    }
}
