using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;
using AncientFireAxe = tsorcRevamp.Content.Items.Weapons.Melee.Axes.AncientFireAxe;
using AncientFireAxeForm = tsorcRevamp.Content.Items.Weapons.Melee.Axes.AncientFireAxeForm;

namespace tsorcRevamp.Content.Projectiles.Melee.Axes
{
    /// <summary>
    /// The short Ancient Fire Axe after its held throw. Spins toward the cursor, arcs down under
    /// gravity, and on the first thing it hits (NPC or tile) sets off an <see cref="AncientFireAxeFireBurst"/>.
    /// It then sticks into the first solid tile and waits for its owner to walk over it; while it
    /// exists, AncientFireAxeForm.IsAxeOut blocks both axe forms. ai[0] = state, ai[1] = flight ticks,
    /// ai[2] = burst already spent (0/1). All synced, so tileCollide/CanDamage derive from them.
    /// </summary>
    public class AncientFireAxeThrown : ModProjectile
    {
        private const int FlightState = 0;
        private const int DropState = 1;  // bounced off an NPC: harmless, falling to the ground
        private const int StuckState = 2;

        // Flies straight for this many ticks, then gravity takes over.
        private const int StraightFlightTicks = 12;
        private const float Gravity = 0.35f;
        private const float MaxFallSpeed = 16f;
        private const float SpinSpeed = 0.45f;
        // Thrown into the sky / off into nothing: return it rather than lose the weapon.
        private const int MaxFlightTicks = 600;
        // Owner teleported away (recall, pylon, death respawn): the axe returns to them.
        private const float ReturnDistance = 250f * 16f;
        // How far the blade sinks into the tile it hits, so it reads as embedded.
        private const float EmbedDepth = 8f;
        private const int PickupPadding = 8;

        private int State
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private bool BurstSpent
        {
            get => Projectile.ai[2] != 0f;
            set => Projectile.ai[2] = value ? 1f : 0f;
        }

        // The short axe's own item sprite (AncientFireAxe.png), shared on purpose.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(AncientFireAxe));

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.netImportant = true;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage()
        {
            if (State == FlightState)
            {
                return null;
            }

            return false;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            bool isOwner = Projectile.owner == Main.myPlayer;

            // Lives until picked up; timeLeft is only a keep-alive.
            Projectile.timeLeft = 2;
            Projectile.tileCollide = State != StuckState;

            bool ownerGone = !owner.active || owner.Distance(Projectile.Center) > ReturnDistance;

            if (isOwner && ownerGone)
            {
                Projectile.Kill();
                return;
            }

            if (State == StuckState)
            {
                Projectile.velocity = Vector2.Zero;

                // The tile it was stuck in got mined out: fall again until it finds another.
                Vector2 supportCheckPosition = Projectile.position - new Vector2(4f);
                bool solidSupport = Collision.SolidCollision(supportCheckPosition, Projectile.width + 8, Projectile.height + 8, true);
                bool stillSupported = solidSupport || IsPlatformAt(Projectile.Center);

                if (!stillSupported)
                {
                    State = DropState;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Projectile.ai[1]++;
                bool gravityActive = State == DropState || Projectile.ai[1] > StraightFlightTicks;

                if (gravityActive)
                {
                    Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);
                }

                float spinDirection = 1f;

                if (Projectile.velocity.X < 0f)
                {
                    spinDirection = -1f;
                }

                Projectile.rotation += SpinSpeed * spinDirection;

                if (isOwner && Projectile.ai[1] >= MaxFlightTicks)
                {
                    Projectile.Kill();
                    return;
                }

                // Vanilla tile collision only sees platforms from above (and projectiles fall through
                // them by default), so check the tile under the centre: from any direction, a platform
                // catches the axe. 14px/tick max speed can't carry the centre past a 16px tile row.
                if (IsPlatformAt(Projectile.Center))
                {
                    StickInto(Projectile.velocity, 0f);
                }
            }

            // Walk over it (flight excluded, so it can't be caught straight out of the throw).
            if (isOwner && State != FlightState)
            {
                Rectangle pickupBox = Projectile.Hitbox;
                pickupBox.Inflate(PickupPadding, PickupPadding);

                if (owner.Hitbox.Intersects(pickupBox) && !owner.dead)
                {
                    SoundEngine.PlaySound(SoundID.Grab, owner.Center);

                    // Still wielding the axe: catch it with a quick underhand swing.
                    if (owner.HeldItem.ModItem is AncientFireAxeForm heldAxe)
                    {
                        heldAxe.CatchThrownAxe(owner);
                    }

                    Projectile.Kill();
                    return;
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            // Burning axe: a trail in flight, a slow smoulder once stuck so it's easy to find again.
            int emberChance = State == StuckState ? 8 : 2;

            if (Main.rand.NextBool(emberChance))
            {
                Vector2 emberVelocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -1.4f);
                Dust ember = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.Torch, emberVelocity, 80, default, 1.2f);
                ember.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.05f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, AncientFireAxe.FireOnFireTicks);

            if (!BurstSpent)
            {
                SpawnBurst();
            }

            // Knocked out of the air by the hit: a small bounce back, then fall to the ground to stick.
            State = DropState;
            Projectile.velocity = new Vector2(-Projectile.velocity.X * 0.15f, -3f);
            Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StickInto(oldVelocity, EmbedDepth);
            return false;
        }

        // Active, non-actuated platform tile at a world position.
        private static bool IsPlatformAt(Vector2 worldPosition)
        {
            Tile tile = Framing.GetTileSafely(worldPosition);
            return tile.HasTile && !tile.IsActuated && TileID.Sets.Platforms[tile.TileType];
        }

        // Plants the axe: first-impact burst if it hasn't fired yet, then freeze in place. embedDepth
        // pushes it into a solid tile along its travel; platforms pass 0, because they're thin and a
        // push could carry the centre out the far side, failing the stuck-support check.
        private void StickInto(Vector2 travelVelocity, float embedDepth)
        {
            if (!BurstSpent && Projectile.owner == Main.myPlayer)
            {
                SpawnBurst();
            }

            BurstSpent = true;

            // Head along the travel direction. The item sprite's head is its top-right corner
            // (-45deg at rotation 0), so +45deg points the head along travel.
            Vector2 travelDirection = travelVelocity.SafeNormalize(Vector2.UnitY);
            Projectile.position += travelDirection * embedDepth;
            Projectile.rotation = travelDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.velocity = Vector2.Zero;
            State = StuckState;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.2f }, Projectile.Center);
            Collision.HitTiles(Projectile.position, travelVelocity, Projectile.width, Projectile.height);
        }

        // Owner only (OnHitNPC runs on the owner; OnTileCollide gates it). The burst carries the
        // throw's damage, which already includes the thrown multiplier.
        private void SpawnBurst()
        {
            BurstSpent = true;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<AncientFireAxeFireBurst>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);

            // Faint heat glow over the lit sprite so it still reads in dark caves.
            Color glowColor = new Color(255, 120, 40, 0) * 0.35f;
            Main.EntitySpriteDraw(texture, drawPosition, null, glowColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }

    /// <summary>
    /// Fire burst where the thrown axe first hits: a ring of the same FireBreath flame licks the fire
    /// wave uses, flung outward to <see cref="WidthTiles"/> across. Hits every NPC inside the circle
    /// once during the first DamageTicks.
    /// </summary>
    public class AncientFireAxeFireBurst : ModProjectile
    {
        // Read by the short axe's tooltip.
        public const int WidthTiles = 5;

        private const float Radius = WidthTiles * 16f * 0.5f;
        private const int LifeTicks = 30;
        private const int ExpandTicks = 10;
        private const int DamageTicks = 12;
        private const int LickCount = 12;

        private int Elapsed => LifeTicks - Projectile.timeLeft;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FireBreath));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = LifeTicks;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.4f, 0.08f) * (Projectile.timeLeft / (float)LifeTicks));

            // localAI[0] marks the one-time ignition effects; runs on every client when the burst syncs in.
            if (Projectile.localAI[0] != 0f || Main.dedServ)
            {
                return;
            }

            Projectile.localAI[0] = 1f;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.1f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f }, Projectile.Center);

            for (int i = 0; i < 24; i++)
            {
                Vector2 emberVelocity = Main.rand.NextVector2Circular(5f, 5f);
                int dustType = DustID.Torch;

                if (Main.rand.NextBool(4))
                {
                    dustType = DustID.GoldFlame;
                }

                Dust ember = Dust.NewDustPerfect(Projectile.Center, dustType, emberVelocity, 60, default, Main.rand.NextFloat(1.1f, 1.7f));
                ember.noGravity = true;
            }
        }

        public override bool? CanDamage()
        {
            if (Elapsed <= DamageTicks)
            {
                return null;
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Circle vs rectangle: nearest point of the target's box to the burst centre.
            Vector2 nearestPoint = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.Distance(nearestPoint, Projectile.Center) <= Radius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Knock outward from the burst, not along the (zero) projectile velocity.
            int knockDirection = 1;

            if (target.Center.X < Projectile.Center.X)
            {
                knockDirection = -1;
            }

            modifiers.HitDirectionOverride = knockDirection;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, AncientFireAxe.FireOnFireTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float lifeProgress = Elapsed / (float)LifeTicks;

            // Ease-out expansion: the ring reaches the full radius in ExpandTicks, then hangs and fades.
            float expand = MathHelper.Clamp(Elapsed / (float)ExpandTicks, 0f, 1f);
            float easedExpand = 1f - (1f - expand) * (1f - expand);
            float alpha = 1f - lifeProgress * lifeProgress;

            // Per-burst layout from the synced identity, so every client draws the same ring.
            Terraria.Utilities.UnifiedRandom random = new Terraria.Utilities.UnifiedRandom(Projectile.identity);
            float ringOffset = random.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < LickCount; i++)
            {
                float angle = ringOffset + i / (float)LickCount * MathHelper.TwoPi;
                float lickReach = Radius * random.NextFloat(0.7f, 1f) * easedExpand;
                float lickScale = random.NextFloat(0.8f, 1.25f) * (1f - lifeProgress * 0.4f);
                float spin = random.NextFloat(-0.12f, 0.12f) * Elapsed;

                Vector2 drawPosition = Projectile.Center + angle.ToRotationVector2() * lickReach - Main.screenPosition;
                // Same hot-core to ember-red ramp as the fire wave columns, cooling as the lick flies out.
                Color color = Color.Lerp(new Color(255, 200, 60), new Color(200, 30, 10), easedExpand) * alpha;

                Main.EntitySpriteDraw(texture, drawPosition, null, color, angle + spin, origin, lickScale, SpriteEffects.None, 0);
            }

            // Bright core flash that shrinks as the ring flies out.
            Color coreColor = new Color(255, 220, 120) * (1f - expand);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, coreColor, 0f, origin, 1.6f * (1f - expand * 0.5f), SpriteEffects.None, 0);
            return false;
        }
    }
}
