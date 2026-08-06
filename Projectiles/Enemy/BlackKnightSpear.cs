using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class BlackKnightSpear : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 12;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 0.8f;
            Projectile.tileCollide = true;
            Projectile.width = 12;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Route player impacts through the same death path as tile impacts so the spear breaks visibly.
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[2] == 1f)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                // Was RedKnightVFX.DrawSpearWake; now the shared Black Knight grey wake.
                EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 28f,
                    direction.ToRotation(), new Vector2(84f, 18f), 0.66f);
            }
            return true;
        }

        #region Kill
        public override void OnKill(int timeLeft)
        {
            //int num98 = -1;
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                if (Projectile.ai[2] == 1f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                        (float)RedKnightBurstKind.SpearImpact, 0.85f);
                }
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);

                }
            }
            Projectile.active = false;
        }
        #endregion
    }

}
