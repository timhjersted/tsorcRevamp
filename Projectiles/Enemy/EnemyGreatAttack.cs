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
            // Was RedKnightVFX.DrawSpearWake; now the shared Black Knight grey wake. NOTE: this
            // projectile is ALSO fired by AncientDemonOfTheAbyss, so its great attack changes
            // colour here too — intentional, it was using the same retired crimson wake.
            EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 24f,
                direction.ToRotation(), new Vector2(78f, 24f), 0.72f);
            return true;
        }
    }
}
