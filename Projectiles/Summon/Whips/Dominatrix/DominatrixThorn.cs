using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.Whips.Dominatrix
{
    class DominatrixThorn : ModProjectile
    {
        public const int ExtraUpdates = 3;
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = ExtraUpdates - 1;
            Projectile.penetrate = 2;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.scale = 0.5f;
        }
        public override void AI()
        {
            // Set spriteDirection based on moving left or right. Left -1, right 1
            Projectile.spriteDirection = (Vector2.Dot(Projectile.velocity, Vector2.UnitX) >= 0f).ToDirectionInt();

            // Point towards where it is moving, applied offset for top right of the sprite respecting spriteDirection
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 - MathHelper.PiOver4 * Projectile.spriteDirection;
            if (Projectile.velocity.Y < 3)
            {
                Projectile.velocity.Y += 0.05f;
            }
        }
    }
}