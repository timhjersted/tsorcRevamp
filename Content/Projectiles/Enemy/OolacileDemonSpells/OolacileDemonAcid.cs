using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.OolacileDemonSpells
{
    // The Oolacile Demon's acid, thrown from the green orb in its hand. Two modes in ai[0]:
    //   ModeSpit (0) — Acid Fan shot: straight line, no gravity, dies on tiles / player / timeout.
    //   ModeGlob (1) — Bile Mortar glob: falls at GlobGravity px/tick², and on a floor it leaves an OolacileDemonAcidPool.
    // ai[1] = the pool's damage (globs only). Every death plays the same green splash (OnKill); a timeout fades out first.
    // Sprite: the bio-spit ball this demon always fired (deliberate reuse, not a placeholder). Round, so it just spins.
    public class OolacileDemonAcid : ModProjectile
    {
        public const float ModeSpit = 0f;
        public const float ModeGlob = 1f;
        public const float GlobGravity = 0.25f;
        public const int LifetimeTicks = 150;
        private const int FadeTicks = 15;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyBioSpitBall));

        private bool IsGlob => Projectile.ai[0] == ModeGlob;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            // Gravity before the engine's position += velocity, so a glob launched with BileVelocity lands where it was solved to.
            if (IsGlob)
            {
                Projectile.velocity.Y += GlobGravity;
                Projectile.scale = 1.3f;
            }

            // Spin the way it travels. Projectile.direction stays 0 for a hostile NewProjectile, so read velocity instead.
            float spinDirection = 1f;
            if (Projectile.velocity.X < 0f)
            {
                spinDirection = -1f;
            }

            Projectile.rotation += 0.3f * spinDirection;

            // Fade over the last FadeTicks if it never hit anything.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            Projectile.alpha = (int)(255 * (1f - fade));

            Lighting.AddLight(Projectile.Center, 0.15f * fade, 0.45f * fade, 0.1f * fade);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CursedTorch, 0f, 0f, 60, default, 1.3f * fade);
                trail.noGravity = true;
                trail.velocity = -Projectile.velocity * 0.15f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Pool spawn is server-side; the splash itself is OnKill on every machine.
            if (IsGlob && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPool();
            }

            return true;
        }

        // Snap the pool onto the first solid floor at or under the impact point (a glob that clips a wall slides down to the floor
        // below it). Scans up to 12 tiles; no floor in that range = no pool.
        private void SpawnPool()
        {
            int tileX = (int)(Projectile.Center.X / 16f);
            int startY = (int)((Projectile.Center.Y - 8f) / 16f);

            for (int tileY = startY; tileY < startY + 12; tileY++)
            {
                if (!WorldGen.InWorld(tileX, tileY, 5))
                {
                    return;
                }

                Tile tile = Main.tile[tileX, tileY];
                bool solidFloor = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);

                if (solidFloor)
                {
                    float floorTop = tileY * 16f;
                    Vector2 poolCenter = new Vector2(Projectile.Center.X, floorTop - OolacileDemonAcidPool.PoolHeight * 0.5f);
                    int poolDamage = (int)Projectile.ai[1];
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), poolCenter, Vector2.Zero, ModContent.ProjectileType<OolacileDemonAcidPool>(), poolDamage, 0f, Main.myPlayer);
                    return;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);

            // Splash: a burst of acid that falls (globs splash bigger).
            int dustCount = 18;
            if (IsGlob)
            {
                dustCount = 30;
            }

            for (int i = 0; i < dustCount; i++)
            {
                Dust splash = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.CursedTorch, 0f, 0f, 40, default, 1.6f);
                splash.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f) + new Vector2(0f, -1f);
                splash.noGravity = Main.rand.NextBool(3);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // The demon's old bio-spit debuffs, minus their progression checks (it only exists in Super Hardmode).
            target.AddBuff(BuffID.Blackout, 120, false);
            target.AddBuff(BuffID.Poisoned, 150, false);
            target.AddBuff(BuffID.Venom, 150, false);
            CurseBuildup.ApplyExplicit(target, 10, 18000);
        }
    }
}
