using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Fire crescent used both as Owl Father's direct ranged slash and as the short ground
    /// waves released by Greatfire Breaker. ai[0]: 0 = direct, 1 = ground-following. The visible
    /// "wave" is carried by trailing PuppetFireWaveColumn instances spawned in AI(); this projectile
    /// is the real hitbox plus a single leading-edge ember.</summary>
    public class PuppetGreatfireCrescent : ModProjectile
    {
        public const int DirectMode = 0;
        public const int GroundMode = 1;
        private const float GroundTravelLimit = 10f * 16f;

        private bool GroundFollowing => (int)Projectile.ai[0] == GroundMode;

        // Ground-mode waves get the full 15-tile rising wall the design calls for; the direct throw
        // is airborne and travels toward the player rather than along the ground, so its trail is a
        // shorter wisp scaled to match instead of reaching all the way down to the terrain.
        private const float GroundWaveColumnHeight = 15f * 16f;
        private const float DirectWaveColumnHeight = 6f * 16f;
        private const int WaveColumnSpawnInterval = 4;
        private int _waveColumnTimer;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/FireBreath";

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 54;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (GroundFollowing)
                {
                    Projectile.tileCollide = false;
                    Projectile.timeLeft = 38;
                }
            }

            if (GroundFollowing)
            {
                Projectile.localAI[1] += System.Math.Abs(Projectile.velocity.X);
                if (Projectile.localAI[1] >= GroundTravelLimit)
                {
                    Projectile.Kill();
                    return;
                }

                if (!PuppetGroundDustWave.TryFindGroundY(Projectile.Center.X, Projectile.Bottom.Y, out float groundY))
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.Center = new Vector2(Projectile.Center.X, groundY - Projectile.height * 0.5f);
                Projectile.velocity.Y = 0f;
            }
            else
            {
                Projectile.velocity *= 0.995f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.9f, 0.35f, 0.06f);

            if (!Main.dedServ)
            {
                _waveColumnTimer++;
                if (_waveColumnTimer >= WaveColumnSpawnInterval)
                {
                    _waveColumnTimer = 0;
                    SpawnFireWaveColumn();
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 22f),
                    Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                    Projectile.velocity * 0.12f,
                    80,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.9f, 1.35f));
                ember.noGravity = true;
            }
        }

        /// <summary>The crescent's own leading edge — a single pulsing ember. The actual "wave of
        /// fire" look comes from the trailing <see cref="PuppetFireWaveColumn"/>s spawned in AI();
        /// this just keeps the real hitbox visually anchored to something instead of invisible.</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);
            float pulse = 1f + (float)System.Math.Sin(Main.GameUpdateCount * 0.3f) * 0.08f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Color.Lerp(Color.Gold, Color.OrangeRed, 0.4f) * fade;

            Main.EntitySpriteDraw(
                texture, drawPosition, null, color, Projectile.rotation,
                texture.Size() * 0.5f, 1.4f * pulse, SpriteEffects.None, 0);

            return false;
        }

        /// <summary>Spawns one rising fire-lick column at the crescent's current position, staggered
        /// every <see cref="WaveColumnSpawnInterval"/> ticks so the wave reads as a continuous curtain
        /// trailing the crescent rather than a single static burst.</summary>
        private void SpawnFireWaveColumn()
        {
            float columnHeight = GroundFollowing ? GroundWaveColumnHeight : DirectWaveColumnHeight;
            float fanSpread = (Projectile.velocity.X < 0f ? -1f : 1f) * 14f;
            Vector2 spawnPosition = Projectile.Center;

            if (GroundFollowing)
            {
                if (!PuppetGroundDustWave.TryFindGroundY(Projectile.Center.X, Projectile.Bottom.Y, out float groundY))
                    return;
                spawnPosition = new Vector2(Projectile.Center.X, groundY);
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), spawnPosition, Vector2.Zero,
                ModContent.ProjectileType<PuppetFireWaveColumn>(), 0, 0f, Projectile.owner,
                columnHeight, fanSpread);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 9; i++)
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    Main.rand.NextVector2Circular(3.2f, 3.2f),
                    80,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.9f, 1.4f));
                ember.noGravity = true;
            }
        }
    }
}
