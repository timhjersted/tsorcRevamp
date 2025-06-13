using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Scrolls
{
    public class EnergyStrikeScrollZap : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.extraUpdates = 100;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            NPC closestNPC = Main.npc[(int)Projectile.ai[2]];
            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1;
            //UsefulFunctions.HomeOnEnemy(Projectile, 200, 1, false, 1, false);

            if (Projectile.timeLeft > 100)
            {
                float randFloatX = Main.rand.Next(-5, 6);
                float randIntY = Main.rand.Next(-5, 6);
                Projectile.velocity += new Vector2(randFloatX, randIntY);
            }

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 5f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 projectilepos = Projectile.position;
                    projectilepos -= Projectile.velocity * (i * 0.5f);
                    Projectile.alpha = 255;
                    int num448 = Dust.NewDust(projectilepos, 1, 1, DustID.Electric);
                    Main.dust[num448].noGravity = true;
                    Main.dust[num448].position = projectilepos;
                    Main.dust[num448].scale = (float)Main.rand.Next(30, 60) * 0.013f;
                    Main.dust[num448].velocity *= 0.2f;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Dissolving>(), 2);
        }
    }
}
