using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class ArrowOfBard : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Ammo/ArrowOfBard";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Arrow of Bard");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        public override void SetDefaults() {

            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.height = 10;
            Projectile.penetrate = 2;
            Projectile.damage = 500;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = (float)1;
            Projectile.tileCollide = true;
            Projectile.width = 5;
            aiType = ProjectileID.WoodenArrowFriendly;
            Projectile.aiStyle = 1;
        }

        public override void Kill(int timeLeft) {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Main.PlaySound(SoundID.Dig, Projectile.position);
        }
    }

}
