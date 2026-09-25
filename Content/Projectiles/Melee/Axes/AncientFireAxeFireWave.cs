using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using AncientFireAxe = tsorcRevamp.Content.Items.Weapons.Melee.Axes.AncientFireAxe;

namespace tsorcRevamp.Content.Projectiles.Melee.Axes
{
    /// <summary>
    /// The Ancient Fire Axe's held-attack ground fire wave — a short player-side version of Owl
    /// Father's Greatfire Crescent. This is the ground-hugging leading edge (one spinning ember plus
    /// its own hitbox); the visible wall of fire is the trail of <see cref="AncientFireAxeFireColumn"/>s
    /// it drops behind itself. Spawned by the owning client only; columns are owner-spawned too.
    /// </summary>
    public class AncientFireAxeFireWave : ModProjectile
    {
        // Read by the axe's tooltip, so the "travels N tiles" text can never drift from the wave.
        public const int TravelTiles = 5;
        public const float TravelSpeed = 6f;

        private const float TravelLimit = TravelTiles * 16f;
        // Owl Father's ground waves are 15 tiles tall; a pre-hardmode axe gets a 7-tile wall.
        private const float ColumnHeight = 7f * 16f;
        // At 6px/tick the 80px trip takes ~13 ticks, so a column every 3 ticks gives a 5-column wall.
        private const int ColumnSpawnInterval = 3;
        private const float LeadingFlameSpinSpeed = 0.35f;

        private float TravelledDistance
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FireBreath));

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            // Safety cap only; the travel limit ends the wave long before this.
            Projectile.timeLeft = 40;
        }

        public override void AI()
        {
            // localAI[0] counts ticks alive (1 on the first AI tick).
            Projectile.localAI[0]++;
            int ticksAlive = (int)Projectile.localAI[0];

            if (ticksAlive == 1)
            {
                // Runs on every client when the wave syncs in, so everyone hears it ignite.
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.62f, Pitch = 0.10f }, Projectile.Center);
            }

            TravelledDistance += System.Math.Abs(Projectile.velocity.X);

            if (TravelledDistance >= TravelLimit)
            {
                Projectile.Kill();
                return;
            }

            // Hug the terrain like Owl Father's crescent. No surface nearby (ledge / pit) ends the wave
            // rather than letting fire walk across open air.
            if (!PuppetGroundDustWave.TryFindGroundY(Projectile.Center.X, Projectile.Bottom.Y, out float groundY))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = new Vector2(Projectile.Center.X, groundY - Projectile.height * 0.5f);
            Projectile.velocity.Y = 0f;

            float spinDirection = 1f;

            if (Projectile.velocity.X < 0f)
            {
                spinDirection = -1f;
            }

            Projectile.rotation += LeadingFlameSpinSpeed * spinDirection;
            Lighting.AddLight(Projectile.Center, 0.9f, 0.35f, 0.06f);

            // Columns carry damage, so only the owner spawns them; they sync like any projectile.
            // The first tick drops one right at the axe so the wall visibly starts where the blade struck.
            bool columnDue = (ticksAlive - 1) % ColumnSpawnInterval == 0;

            if (Projectile.owner == Main.myPlayer && columnDue)
            {
                float fanSpread = spinDirection * 14f;
                Vector2 columnBase = new Vector2(Projectile.Center.X, groundY);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), columnBase, Vector2.Zero,
                    ModContent.ProjectileType<AncientFireAxeFireColumn>(), Projectile.damage, Projectile.knockBack,
                    Projectile.owner, ColumnHeight, fanSpread);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 18f),
                    Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                    Projectile.velocity * 0.12f,
                    80,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.9f, 1.35f));
                ember.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, AncientFireAxe.FireOnFireTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float fade = MathHelper.Clamp(1f - TravelledDistance / TravelLimit, 0f, 1f) * 0.6f + 0.4f;
            float pulse = 1f + (float)System.Math.Sin(Main.GameUpdateCount * 0.3f) * 0.08f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Color.Lerp(Color.Gold, Color.OrangeRed, 0.4f) * fade;

            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation,
                texture.Size() * 0.5f, 1.1f * pulse, SpriteEffects.None, 0);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

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

    /// <summary>
    /// Player-friendly copy of Owl Father's rising fire-lick column (same art, rise and hurtboxes),
    /// trailed by <see cref="AncientFireAxeFireWave"/>. Columns share one static immunity per NPC so
    /// standing in the whole wall costs one column hit, not five.
    /// </summary>
    public class AncientFireAxeFireColumn : PuppetFireWaveColumn
    {
        protected override bool HurtsPlayers => false;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesIDStaticNPCImmunity = true;
            // Outlasts a column's ~43-tick life, so one wave's columns can only land once per NPC.
            Projectile.idStaticNPCHitCooldown = 45;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Columns don't move, so knock away from the axe along the wave's fan direction.
            int knockDirection = 1;

            if (Projectile.ai[1] < 0f)
            {
                knockDirection = -1;
            }

            modifiers.HitDirectionOverride = knockDirection;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, AncientFireAxe.FireOnFireTicks);
        }
    }
}
