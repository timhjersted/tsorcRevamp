using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Close-range melee fire burst used by the FireLurker.
    /// Spawned by the attack scheduler when a player is within ~6 tiles.
    ///
    /// Lifecycle (total ~100 ticks):
    ///   Ticks 100-61 — Telegraph phase (40t): projectile tracks the NPC's waist position and emits
    ///                   warning dust. No hitbox active.
    ///   Ticks 60-1   — Burst phase (60t): locks in place, becomes a ~3×3 tile damaging flame zone.
    ///                   Four flame sprites orbit the center; fire dusts drift upward; fades at end.
    ///   OnKill       — Ring of fire + smoke dust.
    ///
    /// ai[0] = spawning NPC's whoAmI index, used to track waist position during the telegraph.
    /// </summary>
    class FireLurkerFireBurst : ModProjectile
    {
        const int TelegraphTicks = 40;
        const int BurstTicks = 60;

        // Reuse the existing orb spritesheet for the four orbiting flame sprites drawn in PostDraw.
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/FireLurkerFlameOrb";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6; // match FireLurkerFlameOrb frame count
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;   // ~3 tiles
            Projectile.height = 48;  // ~3 tiles
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TelegraphTicks + BurstTicks;
            Projectile.alpha = 255; // sprite always invisible — all visuals via PostDraw and dust
        }

        // Position is managed manually in AI() so it tracks the NPC during the telegraph phase.
        public override bool ShouldUpdatePosition() => false;

        // Only active during the burst phase.
        public override bool? CanDamage() => Projectile.timeLeft <= BurstTicks ? null : false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * (Projectile.timeLeft <= BurstTicks ? 1.1f : 0.4f));

            if (Projectile.timeLeft > BurstTicks)
            {
                // Telegraph phase: follow the NPC's waist so the warning dust sticks to them.
                TrackOwnerWaist();
                EmitTelegraphDust();
            }
            else
            {
                // Burst phase: locked in place, emit flame dust.
                EmitBurstDust();
            }
        }

        void TrackOwnerWaist()
        {
            int owner = (int)Projectile.ai[0];
            if (owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].active)
            {
                // Center at the NPC's waist (slightly below body center).
                Projectile.Center = Main.npc[owner].Center + new Vector2(0, 6);
            }
        }

        void EmitTelegraphDust()
        {
            if (Main.dedServ) return;

            // Pulse intensity builds over the 40-tick wind-up.
            float progress = 1f - (Projectile.timeLeft - BurstTicks) / (float)TelegraphTicks; // 0→1
            int count = 1 + (int)(progress * 2f);

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(14f, 10f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.4f, -0.4f) * (0.6f + progress * 0.8f)),
                    120,
                    new Color(255, (int)(60 + progress * 50), 0),
                    Main.rand.NextFloat(0.7f, 1.2f + progress * 0.5f));
                d.noGravity = true;
            }
        }

        void EmitBurstDust()
        {
            if (Main.dedServ) return;

            float fade = Projectile.timeLeft < 20 ? Projectile.timeLeft / 20f : 1f;
            int count = 1 + (int)(fade * 3f);

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(20f, 20f);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.6f, -0.5f));
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    DustID.Torch,
                    vel,
                    80,
                    new Color(255, (int)(110 + fade * 45), 10),
                    Main.rand.NextFloat(0.9f, 1.7f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Only draw the rotating flame sprites during the burst phase.
            if (Projectile.timeLeft > BurstTicks) return false;

            int orbType = ModContent.ProjectileType<FireLurkerFlameOrb>();
            Texture2D texture = TextureAssets.Projectile[orbType].Value;
            int frameCount = Main.projFrames[orbType] > 0 ? Main.projFrames[orbType] : 1;
            int frameHeight = texture.Height / frameCount;
            int frame = (int)((Main.GameUpdateCount / 4) % (ulong)frameCount);
            Rectangle src = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);

            // Fade out the last 20 ticks to simulate dying fire.
            float fade = Projectile.timeLeft < 20 ? Projectile.timeLeft / 20f : 1f;
            Color color = lightColor * fade;

            // Four sprites rotate slowly around the burst center, each offset by 90°.
            float baseAngle = Main.GameUpdateCount * 0.055f;
            const float orbitRadius = 13f;

            for (int i = 0; i < 4; i++)
            {
                float angle = baseAngle + i * MathHelper.PiOver2;
                Vector2 offset = new Vector2(orbitRadius, 0).RotatedBy(angle);
                Vector2 drawPos = Projectile.Center + offset - Main.screenPosition;
                // Rotate each sprite to face outward from center.
                Main.spriteBatch.Draw(texture, drawPos, src, color,
                    angle + MathHelper.PiOver2,
                    src.Size() / 2f,
                    1.1f, SpriteEffects.None, 0f);
            }

            return false; // suppress default (invisible) sprite draw
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;

            // Outward fire ring.
            for (int i = 0; i < 24; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 24f);
                Dust fire = Dust.NewDustPerfect(
                    Projectile.Center + dir * Main.rand.NextFloat(4f, 14f),
                    DustID.Torch,
                    dir * Main.rand.NextFloat(4f, 9f),
                    60, new Color(255, 130, 20),
                    Main.rand.NextFloat(1.2f, 2.1f));
                fire.noGravity = true;
                fire.fadeIn = 0.8f;
            }

            // Smoke wisps.
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 12f);
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center + dir * Main.rand.NextFloat(3f, 10f),
                    DustID.Smoke,
                    dir * Main.rand.NextFloat(2f, 5f) + new Vector2(0, -1f),
                    150, new Color(80, 60, 45),
                    Main.rand.NextFloat(1.3f, 2.2f));
                smoke.noGravity = true;
                smoke.fadeIn = 0.7f;
            }
        }
    }
}
