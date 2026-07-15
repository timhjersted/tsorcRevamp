using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Straight-line crescent slash, fired toward wherever the player was at release (no homing).
    // The source sprite is a plain white 4-frame crescent (170x170/frame) - tinted purple via
    // GetAlpha, scaled down small ("tiny"), with a purple point light and dust trail.
    class AbyssSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssSlash";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.scale = 0.35f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.6f;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);

            Animate();

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool white = Main.rand.NextBool(5);
                    Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(18f, 12f) * Projectile.scale;
                    Dust d = Dust.NewDustPerfect(position,
                        white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                        -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.16f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
                        90, white ? Color.White : Color.DarkViolet, Main.rand.NextFloat(0.85f, 1.25f));
                    d.noGravity = true;
                }
            }
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(190, 90, 255, 220);
        }

        // ai[0] holds the firing NPC's whoAmI + 1 (0 = "no owner") so the dodge-punish-chain system
        // knows this swipe actually connected. The +1 offset exists because this projectile is ALSO
        // reused anonymously (no owner) by Spiral Fan's straight-shot bursts, which have nothing to
        // do with that system and must not accidentally report a hit against whatever NPC happens
        // to occupy slot 0.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int ownerIdx = (int)Projectile.ai[0] - 1;
            if (ownerIdx >= 0 && ownerIdx < Main.maxNPCs && Main.npc[ownerIdx].active
                && Main.npc[ownerIdx].ModNPC is PuppetNPC invader)
            {
                invader.ReportAttackHit();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, default, 1f);
                d.noGravity = true;
            }
        }
    }
}
