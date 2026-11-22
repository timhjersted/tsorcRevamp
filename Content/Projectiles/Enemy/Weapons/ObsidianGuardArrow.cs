using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class ObsidianGuardArrow : ModProjectile
    {
        // Intentional permanent art sharing: physical arrow behavior does not inherit Unholy Arrow AI or piercing.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.UnholyArrow;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage()
        {
            return Projectile.timeLeft > 8;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.timeLeft < 15)
            {
                Projectile.alpha = (15 - Projectile.timeLeft) * 255 / 15;
            }

            if (!Main.dedServ && Projectile.timeLeft % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DemonTorch,
                    -Projectile.velocity * 0.06f, 160, default, 0.55f);
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.45f }, Projectile.Center);

            // Small physical arrows get a restrained chip burst, rather than a spell-sized explosion in each corridor.
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Obsidian,
                    Main.rand.NextVector2Circular(2.2f, 2.2f), 100, default, 0.8f);
                dust.noGravity = false;
            }
        }
    }
}
