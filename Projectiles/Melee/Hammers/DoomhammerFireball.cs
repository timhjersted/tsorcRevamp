using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Axes;

namespace tsorcRevamp.Projectiles.Melee.Hammers
{
    class DoomhammerFireball : ModProjectile
    {
        public int ProjectileLifetime = 60;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 66;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.tileCollide = true; 
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.CritChance = (int)Projectile.ai[0];
            Projectile.ai[0] = 0;
        }

        public override void AI()
        {
            for (int num88 = 0; num88 < 2; num88++)
            {
                int num89 = Dust.NewDust(
                    new Vector2(Projectile.position.X, Projectile.position.Y),
                    Projectile.width,
                    Projectile.height,
                    DustID.Torch, 
                    Projectile.velocity.X * 0.2f,
                    Projectile.velocity.Y * 0.2f,
                    100,
                    default(Color),
                    1.5f
                );
                Main.dust[num89].noGravity = true;
                Main.dust[num89].velocity *= 0.3f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, Projectile.light * 0.4f, Projectile.light * 0.1f, Projectile.light * 1f);

            Projectile.frameCounter++;
            int frameSpeed = 5;
            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire3, 5 * 60);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Torch,
                    Main.rand.NextFloat(-2f, 2f),
                    Main.rand.NextFloat(-2f, 2f),
                    100,
                    default(Color),
                    1.5f
                );
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AncientFireAxeFireballBurst>(),
                    Projectile.damage / 2,
                    Projectile.knockBack,
                    Main.myPlayer,
                    Projectile.CritChance
                );
            }
        }
    }
}