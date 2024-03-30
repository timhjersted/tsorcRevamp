using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class ThornDecapitatorThorn : ModProjectile
    {
        public int Frames = 1;
        public const int ExtraUpdates = 2;
        public int ProjectileLifetime = 360 * ExtraUpdates;
        public Vector2 CircularEdge;
        public int Timer = 0;
        public int CircleSize = 100;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.extraUpdates = ExtraUpdates - 1;
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Melee;
            CircularEdge = Main.rand.NextVector2CircularEdge(CircleSize, CircleSize);
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.damage /= 2;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.velocity = (Projectile.DirectionTo(player.Center + CircularEdge) * 5) + player.velocity / 2;
            Timer++;
            if (Timer > 60 || Projectile.Center.Distance(player.Center + CircularEdge) < 5)
            {
                CircularEdge = Main.rand.NextVector2CircularEdge(CircleSize, CircleSize);
                Timer = 0;
            }
            // Set spriteDirection based on moving left or right. Left -1, right 1
            Projectile.spriteDirection = (Vector2.Dot(Projectile.velocity, Vector2.UnitX) >= 0f).ToDirectionInt();

            // Point towards where it is moving, applied offset for top right of the sprite respecting spriteDirection
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 - MathHelper.PiOver4 * Projectile.spriteDirection;
            Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.JungleSpore, 0, 0, 0, Color.Red, 0.2f);
        }
    }
}