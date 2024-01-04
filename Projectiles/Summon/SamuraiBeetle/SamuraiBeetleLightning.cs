using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.SamuraiBeetle
{
    public class SamuraiBeetleLightning : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.extraUpdates = 1000;
            Projectile.timeLeft = 1080;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 5f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 projectilepos = Projectile.position;
                    projectilepos -= Projectile.velocity * (i * 0.5f);
                    Projectile.alpha = 255;
                    int num448 = Dust.NewDust(projectilepos, 1, 1, 70, newColor: Color.Purple);
                    Main.dust[num448].noGravity = true;
                    Main.dust[num448].position = projectilepos;
                    Main.dust[num448].scale = (float)Main.rand.Next(70, 110) * 0.013f;
                    Main.dust[num448].velocity *= 0.2f;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CCShock>(), 600);
        }
    }
}
