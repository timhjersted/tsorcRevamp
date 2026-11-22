using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class ForgottenKnightSpear : BlackKnightSpear
    {
        internal const float SpriteSize = 80f;
        internal static readonly Vector2 HeadTexel = new Vector2(79f, 1f);
        internal static readonly Vector2 BaseTexel = new Vector2(3f, 77f);

        // Match the previous 61px shaft so the calibrated hand grips and reach
        // continue to follow the pearl spear's measured tip and butt.
        internal static readonly float HeldScale = 61f / Vector2.Distance(HeadTexel, BaseTexel);
        internal static string SpritePath => UsefulFunctions.RefactorableFilepath(typeof(EnemyForgottenPearlSpearProj));

        public override string Texture => SpritePath;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.scale *= HeldScale;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.spriteDirection = 1;

            if (Projectile.timeLeft <= 15)
            {
                Projectile.hostile = false;
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / 15f));
            }
            if (!Main.dedServ && Projectile.timeLeft % 3 == 0)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch,
                    -Projectile.velocity * 0.08f, 80, Color.LightBlue, 0.9f);
                mote.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(2)) target.AddBuff(BuffID.Weak, 60 * 60);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 40; i++)
            {
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch,
                    Main.rand.NextVector2Circular(4f, 4f), 80, Color.LightBlue, 1.2f);
                mote.noGravity = true;
            }
        }
    }
}
