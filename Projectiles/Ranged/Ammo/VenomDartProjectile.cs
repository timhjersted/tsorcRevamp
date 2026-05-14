using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class VenomDartProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            AIType = ProjectileID.PoisonDartBlowgun;
            Projectile.CloneDefaults(ProjectileID.PoisonDartBlowgun);
            Projectile.friendly = true;
            Projectile.height = 15;
            Projectile.tileCollide = true;
            Projectile.width = 15;
            Projectile.DamageType = DamageClass.Ranged; 
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Projectile.velocity.Y;

            Lighting.AddLight(Projectile.Center, 0.2f, 0.4f, 1f);

            if (Main.rand.NextBool(8))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    171,
                    Projectile.velocity.X * 0.2f,
                    Projectile.velocity.Y * 0.2f,
                    100,
                    default,
                    1.4f
                );

                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 4 * 60);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int radius = 80; 

            target.AddBuff(BuffID.Venom, 4 * 60);

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.whoAmI != target.whoAmI)
                {
                    float distance = Vector2.Distance(npc.Center, target.Center);

                    if (distance <= radius)
                    {
                        npc.AddBuff(BuffID.Venom, 4 * 60);
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
                    171,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(1.8f, 2.1f)
                );
                dust.noGravity = true;
            }

            Projectile.penetrate = 8;
            Vector2 oldCenter = Projectile.Center;
            Projectile.width = 135;
            Projectile.height = 135;
            Projectile.position = oldCenter - new Vector2(Projectile.width / 2f, Projectile.height / 2f);
            Projectile.ArmorPenetration = 20;
            Projectile.damage /= 5;
            Projectile.Damage();
        }
    }

}
