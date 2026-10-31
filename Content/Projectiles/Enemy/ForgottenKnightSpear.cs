using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class ForgottenKnightSpear : BlackKnightSpear
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(BlackKnightSpear));

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
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
