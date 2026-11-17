using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDrake;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class DragonMeteor : ModProjectile
    {
        private const int DrakeLifetime = 128;
        private bool retiring;
        private bool FromDrake => Projectile.ai[2] > 0f;

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            AIType = 20;
            Projectile.width = 59;
            Projectile.height = 62;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 600;
            Projectile.light = 1;
            Projectile.tileCollide = true;
            Projectile.friendly = false;
            Projectile.hostile = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (FromDrake && (retiring || timeLeft <= 0))
            {
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 drift = Main.rand.NextVector2Circular(2.8f, 2.8f);
                        Dust.NewDustPerfect(Projectile.Center, DustID.Torch, drift, 100, default, 0.8f).noGravity = true;
                    }
                }
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float impactStyle = FromDrake ? DragonMeteorExplosion.DrakeImpact : 0f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<DragonMeteorExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner,
                    ai0: impactStyle);
            }
        }

        public override bool PreAI()
        {
            if (!FromDrake)
            {
                if (!Main.dedServ)
                {
                    Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
                        Projectile.velocity.X, Projectile.velocity.Y, 200, default, 3f).noGravity = true;
                    Lighting.AddLight(Projectile.Center, 1f, 1f, 1f);
                }
                Projectile.rotation += 0.1f;
                return true;
            }

            // The drake fixes the launch vector at spawn. Skip the old generic projectile AI for this variant.
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, DrakeLifetime);
            Projectile.light = 0f;
            int ownerIndex = (int)Projectile.ai[2] - 1;
            NPC owner = null;
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                owner = Main.npc[ownerIndex];
            }
            bool activeDrake = owner is { active: true, ModNPC: HellkiteDrake };
            bool ownerRetiring = owner is { active: true, ModNPC: HellkiteDrake drake }
                && (drake.State == HellkiteDrake.DrakeState.Collapse || owner.ai[3] != 0f);
            if (ownerRetiring || (Main.netMode != NetmodeID.MultiplayerClient && !activeDrake))
            {
                retiring = true;
                Projectile.hostile = false;
                Projectile.tileCollide = false;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 12);
            }

            float fadeIn = Math.Clamp((DrakeLifetime - Projectile.timeLeft + 1f) / 7f, 0f, 1f);
            float fadeOut = Math.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            float visibility = fadeIn * fadeOut;
            Projectile.alpha = (int)(255f * (1f - visibility));
            Projectile.rotation += 0.14f;
            if (Main.dedServ)
            {
                return false;
            }

            Lighting.AddLight(Projectile.Center, 1f * visibility, 0.31f * visibility, 0.06f * visibility);
            Vector2 wake = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 23f;
            for (int i = 0; i < 2; i++)
            {
                Vector2 origin = wake + Main.rand.NextVector2Circular(16f, 11f);
                Vector2 drift = -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1.5f, 1.5f);
                Dust cinder = Dust.NewDustPerfect(origin, DustID.Torch, drift, 90, default, Main.rand.NextFloat(1.1f, 1.7f) * visibility);
                cinder.noGravity = true;
            }
            if (Main.rand.NextBool(3))
            {
                Vector2 drift = -Projectile.velocity * 0.12f;
                Dust.NewDustPerfect(wake, DustID.Smoke, drift, 125, new Color(96, 62, 47), 1.25f * visibility).noGravity = true;
            }
            return false;
        }
    }
}
