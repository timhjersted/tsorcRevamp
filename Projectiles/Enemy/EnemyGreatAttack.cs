using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyGreatAttack : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Great Attack");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            Projectile.hostile = true;
            Projectile.height = 5;
            Projectile.light = 1f;
            Projectile.penetrate = 4;
            Projectile.scale = 1;
            Projectile.tileCollide = true;
            Projectile.width = 5;
            Projectile.alpha = 200;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 20;
        }
        public override bool PreKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                    (float)RedKnightBurstKind.BombExplosion, 0.8f);
            }
            Projectile.type = 102;
            //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            RedKnightVFX.DrawSpearWake(Projectile.Center - direction * 24f,
                direction.ToRotation(), new Vector2(72f, 26f), 0.8f, empowered: true);
            return true;
        }
    }
}
