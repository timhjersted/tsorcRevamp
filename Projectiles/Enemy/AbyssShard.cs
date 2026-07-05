using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Purple/white recolor of Deerclops's ice spike (ProjectileID.DeerclopsIceSpike). Reuses the same
    // shape that made the original read well: spawned at an approximate ground X, it snaps itself down
    // onto the actual solid surface, sits through a dust-only telegraph window with no hitbox, then
    // "pops" (extra dust burst + tiny screenshake) into a brief active/damaging window before fading
    // out and dying - entirely self-contained, so the boss side only has to decide where and when to
    // spawn one of these.
    class AbyssShard : ModProjectile
    {
        const int TelegraphTicks = 40;
        const int PopHoldTicks   = 10;
        const int FadeTicks      = 10;
        const int TotalTicks     = TelegraphTicks + PopHoldTicks + FadeTicks;

        bool _grounded;
        bool _popped;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssShard";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = TotalTicks;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
        }

        public override void AI()
        {
            if (!_grounded)
            {
                SnapToGround();
                _grounded = true;
            }

            int elapsed = TotalTicks - Projectile.timeLeft;

            if (elapsed < TelegraphTicks)
            {
                Projectile.alpha = 255;
                if (!Main.dedServ && elapsed % 3 == 0)
                {
                    SpawnTelegraphDust();
                }
                return;
            }

            if (!_popped)
            {
                _popped = true;
                Pop();
            }

            int sincePop = elapsed - TelegraphTicks;
            if (sincePop < PopHoldTicks)
            {
                Projectile.alpha = 0;
            }
            else
            {
                float fadeT = (sincePop - PopHoldTicks) / (float)FadeTicks;
                Projectile.alpha = (int)MathHelper.Clamp(fadeT * 255f, 0f, 255f);
            }
        }

        // Deerclops finds ground by walking the target's column; we don't have easy access to a
        // player reference here, so just probe straight down (then up as a fallback) from the
        // spawn point - good enough since the boss side already spawns roughly at ground height.
        void SnapToGround()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int tileY = (int)(Projectile.Center.Y / 16f);
            int foundY = -1;

            for (int i = 0; i < 60; i++)
            {
                int y = tileY + i;
                if (WorldGen.InWorld(tileX, y) && WorldGen.SolidTile(tileX, y))
                {
                    foundY = y;
                    break;
                }
            }

            if (foundY < 0)
            {
                for (int i = 1; i < 60; i++)
                {
                    int y = tileY - i;
                    if (WorldGen.InWorld(tileX, y) && WorldGen.SolidTile(tileX, y))
                    {
                        foundY = y;
                        break;
                    }
                }
            }

            if (foundY >= 0)
            {
                Projectile.Bottom = new Vector2(tileX * 16f + 8f, foundY * 16f);
            }
        }

        void SpawnTelegraphDust()
        {
            Vector2 pos = Projectile.Bottom - new Vector2(0f, 4f);
            Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : Color.White;
            Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(10f, 4f), DustID.PurpleTorch,
                new Vector2(0f, -0.6f), 100, tint, Main.rand.NextFloat(0.8f, 1.3f));
            d.noGravity = true;
        }

        void Pop()
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.1f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, strength: 2.5f, frames: 8);

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 18; i++)
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : Color.White;
                Vector2 vel = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f));
                Dust d = Dust.NewDustPerfect(Projectile.Bottom, DustID.PurpleTorch, vel, 60, tint, Main.rand.NextFloat(1.1f, 1.7f));
                d.noGravity = true;
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            return elapsed >= TelegraphTicks;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false);
            target.AddBuff(BuffID.Bleeding, 16 * 60, false);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 16 * 60, false);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 140, 255, 255 - Projectile.alpha);
        }
    }
}
