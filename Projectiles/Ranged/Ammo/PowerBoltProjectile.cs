using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class PowerBoltProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.height = 20;
            Projectile.penetrate = 2;
            Projectile.tileCollide = true;
            Projectile.width = 10;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

            if (Main.rand.NextFloat() < 0.33f)
            {
                Explode();
            }
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 1) 
            {
                Projectile.localAI[0] = 0; 
                Explode();
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.rand.NextFloat() < 0.33f)
            {
                Projectile.localAI[0] = 1;
            }
        }

        private void Explode()
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PowerBoltExplosion>(), Projectile.damage / 3, 0f, Projectile.owner);
        }
    }

    public class PowerBoltExplosion : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 160;   
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.hide = true;
        }

        public override void AI()
        {
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(23f, 23f),
                    DustID.Smoke,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(2.0f, 2.4f)
                );
                dust.noGravity = true;
            }

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(21f, 21f),
                    DustID.Torch,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(2.0f, 2.3f)
                );
                dust.noGravity = true;
            }
        }
    }
}