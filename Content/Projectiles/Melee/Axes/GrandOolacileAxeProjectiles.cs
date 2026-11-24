using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Axes;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Projectiles.Melee.Axes
{
    /// <summary>
    /// Right-click controller for GrandOolacileAxe (modelled on GravemawController).
    /// Tap right-click (released within 18 ticks): Destined Axe Cast (thrown spinning axe).
    /// Hold right-click (held past 18 ticks): Staff-Axe Weave (channelled 3-bolt homing dark barrage).
    /// </summary>
    public class GrandOolacileAxeController : ModProjectile
    {
        private const int TapWindow = 18;
        private const int WeaveIntervalTicks = 26;
        private const float ThrownAxeDamageMult = 1.15f;

        private bool castFinished;
        private int weaveCooldown;
        private Vector2 aimDirection = Vector2.UnitX;

        private float EffectiveUseTime
        {
            get
            {
                if (Projectile.ai[0] > 0f)
                {
                    return Projectile.ai[0];
                }

                return 30f;
            }
        }

        private int UseTicks => Math.Max(1, (int)EffectiveUseTime);

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyOccultistAxe));

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(castFinished);
            writer.Write(weaveCooldown);
            writer.WriteVector2(aimDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            castFinished = reader.ReadBoolean();
            weaveCooldown = reader.ReadInt32();
            aimDirection = reader.ReadVector2();
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            if (player.HeldItem.type != ModContent.ItemType<GrandOolacileAxe>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = player.Center;
            Projectile.timeLeft = 60;
            Projectile.localAI[0]++;
            float timer = Projectile.localAI[0];

            bool channeling = player.controlUseTile;

            if (castFinished)
            {
                int remaining = Math.Max(channeling ? 2 : 0, UseTicks - (int)timer);
                if (Main.myPlayer == Projectile.owner)
                {
                    player.itemTime = remaining;
                    player.itemAnimation = remaining;
                }

                if (Main.myPlayer == Projectile.owner && !channeling && timer >= UseTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (channeling)
            {
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;

                if (Main.myPlayer == Projectile.owner)
                {
                    Vector2 newAim = (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(player.direction, 0f));
                    if (Vector2.DistanceSquared(newAim, aimDirection) > 0.001f)
                    {
                        aimDirection = newAim;
                        Projectile.netUpdate = true;
                    }

                    int newDir = Main.MouseWorld.X >= player.Center.X ? 1 : -1;
                    player.ChangeDir(newDir);
                }

                player.SetCompositeArmFront(
                    true,
                    Player.CompositeArmStretchAmount.Full,
                    aimDirection.ToRotation() - MathHelper.PiOver2
                );
            }

            if (timer <= TapWindow)
            {
                if (!channeling)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        TriggerTapCast(player);
                        castFinished = true;
                        Projectile.netUpdate = true;
                    }
                }

                return;
            }

            if (!channeling)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    castFinished = true;
                    Projectile.netUpdate = true;
                }

                return;
            }

            RunWeaveHold(player);
        }

        private void TriggerTapCast(Player player)
        {
            if (GrandOolacileAxe.IsAxeOut(player))
            {
                return;
            }

            Vector2 targetAim = (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(player.direction, 0f));
            Vector2 throwVelocity = targetAim * 16f;
            throwVelocity.Y -= 2.5f;

            int throwDamage = (int)(Projectile.damage * ThrownAxeDamageMult);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                player.Center,
                throwVelocity,
                ModContent.ProjectileType<GrandOolacileThrownAxe>(),
                throwDamage,
                Projectile.knockBack,
                Projectile.owner
            );

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.85f, Pitch = -0.25f }, player.Center);
            }
        }

        private void RunWeaveHold(Player player)
        {
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 headPos = player.Center + aimDirection * 56f + Main.rand.NextVector2Circular(12f, 12f);
                int dustType = Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.CrimsonSpray;
                Dust swirl = Dust.NewDustPerfect(
                    headPos,
                    dustType,
                    -aimDirection * Main.rand.NextFloat(0.6f, 1.8f),
                    80,
                    default,
                    Main.rand.NextFloat(0.75f, 1.2f)
                );
                swirl.noGravity = true;
            }

            if (weaveCooldown > 0)
            {
                weaveCooldown--;
                return;
            }

            weaveCooldown = WeaveIntervalTicks;

            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 targetAim = (Main.MouseWorld - player.Center).SafeNormalize(new Vector2(player.direction, 0f));
                FireWeaveFan(player, targetAim);
                Projectile.netUpdate = true;
            }
        }

        private void FireWeaveFan(Player player, Vector2 centerDirection)
        {
            if (!player.CheckMana(GrandOolacileAxe.WeaveManaCost, pay: true))
            {
                castFinished = true;
                return;
            }

            player.manaRegenDelay = (int)player.maxRegenDelay;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f, Pitch = 0.1f }, player.Center);
            }

            Vector2 spawnPos = player.Center + centerDirection * 48f;
            float baseAngle = centerDirection.ToRotation();
            int boltDamage = Math.Max(1, Projectile.damage / GrandOolacileAxe.WeaveBoltCount);
            int halfSpread = GrandOolacileAxe.WeaveBoltCount / 2;

            for (int index = -halfSpread; index <= halfSpread; index++)
            {
                float angle = baseAngle + MathHelper.ToRadians(index * 12f);
                Vector2 boltVelocity = angle.ToRotationVector2() * 9.5f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    boltVelocity,
                    ModContent.ProjectileType<GrandOolacileDarkBolt>(),
                    boltDamage,
                    Projectile.knockBack * 0.4f,
                    Projectile.owner
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (castFinished || Projectile.localAI[0] <= TapWindow)
            {
                return false;
            }

            Player player = Main.player[Projectile.owner];
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            SpriteEffects effects = player.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            float rotationOffset = player.direction == -1 ? MathHelper.Pi - MathHelper.PiOver4 : MathHelper.PiOver4;
            float drawRotation = aimDirection.ToRotation() + rotationOffset;
            Vector2 origin = player.direction == -1
                ? new Vector2(texture.Width * 0.75f, texture.Height * 0.75f)
                : new Vector2(texture.Width * 0.25f, texture.Height * 0.75f);

            Vector2 drawPosition = player.MountedCenter - Main.screenPosition + new Vector2(0f, player.gfxOffY);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                lightColor,
                drawRotation,
                origin,
                0.85f,
                effects,
                0
            );

            return false;
        }
    }

    /// <summary>
    /// Option A: Friendly thrown Grand Oolacile Axe.
    /// Explodes on first enemy or tile impact into a Destined Death burst and spreading flame columns,
    /// then embeds in the ground until its owner walks over it (triggering an underhand catch swing).
    /// ai[0] = state (0 flight, 1 drop, 2 stuck), ai[1] = flight ticks, ai[2] = burst spent (0/1).
    /// </summary>
    public class GrandOolacileThrownAxe : ModProjectile
    {
        private const int FlightState = 0;
        private const int DropState = 1;
        private const int StuckState = 2;

        private const int StraightFlightTicks = 12;
        private const float Gravity = 0.30f;
        private const float MaxFallSpeed = 16f;
        private const float SpinSpeed = 0.36f;
        private const int MaxFlightTicks = 600;
        private const float ReturnDistance = 250f * 16f;
        private const float EmbedDepth = 10f;
        private const int PickupPadding = 12;

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

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyOccultistAxe));

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.scale = 0.75f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.netImportant = true;
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
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);
                }

                float spinDirection = Projectile.velocity.X < 0f ? -1f : 1f;
                Projectile.rotation += SpinSpeed * spinDirection;

                if (isOwner && Projectile.ai[1] >= MaxFlightTicks)
                {
                    Projectile.Kill();
                    return;
                }

                if (IsPlatformAt(Projectile.Center))
                {
                    StickInto(Projectile.velocity, 0f);
                }
            }

            if (isOwner && State != FlightState)
            {
                Rectangle pickupBox = Projectile.Hitbox;
                pickupBox.Inflate(PickupPadding, PickupPadding);

                if (owner.Hitbox.Intersects(pickupBox) && !owner.dead)
                {
                    SoundEngine.PlaySound(SoundID.Grab, owner.Center);

                    if (owner.HeldItem.ModItem is GrandOolacileAxe heldAxe)
                    {
                        heldAxe.CatchThrownAxe(owner);
                    }

                    Projectile.Kill();
                    return;
                }
            }

            if (!Main.dedServ)
            {
                int emberChance = State == StuckState ? 7 : 2;
                if (Main.rand.NextBool(emberChance))
                {
                    int dustType = Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch;
                    Vector2 dustVel = State == StuckState
                        ? new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -1.2f)
                        : -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.14f);
                    Dust trail = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                        dustType,
                        dustVel,
                        30,
                        new Color(255, 220, 45),
                        Main.rand.NextFloat(0.65f, 1.05f)
                    );
                    trail.noGravity = true;
                }

                Lighting.AddLight(Projectile.Center, 0.75f, 0.45f, 0.12f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 300);

            if (!BurstSpent)
            {
                Detonate(target.Bottom);
            }

            State = DropState;
            Projectile.velocity = new Vector2(-Projectile.velocity.X * 0.15f, -3.5f);
            Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StickInto(oldVelocity, EmbedDepth);
            return false;
        }

        private static bool IsPlatformAt(Vector2 worldPosition)
        {
            Tile tile = Framing.GetTileSafely(worldPosition);
            return tile.HasTile && !tile.IsActuated && TileID.Sets.Platforms[tile.TileType];
        }

        private void StickInto(Vector2 travelVelocity, float embedDepth)
        {
            if (!BurstSpent)
            {
                Detonate(Projectile.Bottom - new Vector2(0f, 4f));
            }

            Vector2 travelDirection = travelVelocity.SafeNormalize(Vector2.UnitY);
            Projectile.position += travelDirection * embedDepth;
            Projectile.rotation = travelDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.velocity = Vector2.Zero;
            State = StuckState;
            Projectile.netUpdate = true;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.8f, Pitch = -0.25f }, Projectile.Center);
                Collision.HitTiles(Projectile.position, travelVelocity, Projectile.width, Projectile.height);
            }
        }

        private void Detonate(Vector2 groundZero)
        {
            if (BurstSpent)
            {
                return;
            }

            BurstSpent = true;
            Projectile.netUpdate = true;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.35f }, groundZero);

                for (int i = 0; i < 42; i++)
                {
                    Vector2 direction = Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi).ToRotationVector2();
                    bool blood = i % 3 != 0;
                    Dust body = Dust.NewDustPerfect(
                        groundZero + direction * Main.rand.NextFloat(4f, 64f),
                        blood ? DustID.Blood : DustID.Wraith,
                        direction * Main.rand.NextFloat(1.4f, 5.5f) + new Vector2(0f, -1.2f),
                        blood ? 80 : 155,
                        default,
                        blood ? Main.rand.NextFloat(0.75f, 1.3f) : Main.rand.NextFloat(0.55f, 0.9f)
                    );
                    body.noGravity = true;
                    body.noLight = !blood;
                }
            }

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    groundZero,
                    Vector2.Zero,
                    ModContent.ProjectileType<GrandOolacileDestinedBurst>(),
                    (int)(Projectile.damage * 0.75f),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner,
                    1.25f
                );

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    groundZero,
                    Vector2.Zero,
                    ModContent.ProjectileType<GrandOolacileFlameField>(),
                    (int)(Projectile.damage * 0.45f),
                    0f,
                    Projectile.owner
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            // Faint crimson-gold glow so the embedded axe stays easy to spot.
            Color glowColor = new Color(220, 60, 70, 0) * 0.35f;
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                glowColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }

    /// <summary>
    /// Friendly Destined Death burst reusing DestinedDeathExplosion's 5-frame sprite sheet.
    /// </summary>
    public class GrandOolacileDestinedBurst : ModProjectile
    {
        private const int FrameCount = 5;
        private const int TicksPerFrame = 4;
        private const int Lifetime = FrameCount * TicksPerFrame;

        private float BurstScale => Projectile.ai[0] > 0f ? Projectile.ai[0] : 1f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathExplosion));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 300);
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= FrameCount)
                {
                    Projectile.frame = FrameCount - 1;
                }
            }

            Lighting.AddLight(Projectile.Center, 0.7f, 0.1f, 0.15f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new(texture.Width * 0.5f, frameHeight * 0.5f);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White,
                0f,
                origin,
                BurstScale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }

    /// <summary>
    /// Spawns a center-out row of 9 friendly Destined Death flame columns along nearby ground.
    /// </summary>
    public class GrandOolacileFlameField : ModProjectile
    {
        private static readonly int[] ColumnOffsets =
        {
            0, -1, 1, -2, 2, -3, 3, -4, 4,
        };

        private const int ColumnInterval = 2;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ColumnOffsets.Length * ColumnInterval + 2;
            Projectile.penetrate = -1;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;
        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            int elapsed = ColumnOffsets.Length * ColumnInterval + 2 - Projectile.timeLeft;
            if (elapsed < 0 || elapsed % ColumnInterval != 0)
            {
                return;
            }

            int columnIndex = elapsed / ColumnInterval;
            if (columnIndex >= ColumnOffsets.Length)
            {
                return;
            }

            int tileX = (int)(Projectile.Center.X / 16f) + ColumnOffsets[columnIndex];
            int startY = (int)(Projectile.Center.Y / 16f);

            if (!TryFindGroundColumn(tileX, startY, out int groundY))
            {
                groundY = startY + 1;
            }

            int heightTiles = Main.rand.Next(3, 5);
            Vector2 spawnCenter = new Vector2(tileX * 16f + 8f, groundY * 16f - 8f);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnCenter,
                Vector2.Zero,
                ModContent.ProjectileType<GrandOolacileFlameColumn>(),
                Projectile.damage,
                0f,
                Projectile.owner,
                heightTiles,
                columnIndex
            );
        }

        private static bool TryFindGroundColumn(int x, int impactY, out int groundY)
        {
            groundY = impactY;
            if (x < 5 || x >= Main.maxTilesX - 5)
            {
                return false;
            }

            for (int y = Math.Max(5, impactY - 4); y <= impactY + 12 && y < Main.maxTilesY - 5; y++)
            {
                Tile tile = Framing.GetTileSafely(x, y);
                Tile above = Framing.GetTileSafely(x, y - 1);
                bool standable = tile.HasTile && Main.tileSolid[tile.TileType];
                bool blockedAbove = above.HasTile && Main.tileSolid[above.TileType]
                    && !Main.tileSolidTop[above.TileType];
                if (standable && !blockedAbove)
                {
                    groundY = y;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Friendly persistent Destined Death flame column reusing DestinedDeathBlaze art.
    /// </summary>
    public class GrandOolacileFlameColumn : ModProjectile
    {
        private const int FrameCount = 5;
        private const int TicksPerFrame = 6;
        private const int Lifetime = 4 * 60;

        private int HeightTiles => Math.Clamp((int)Projectile.ai[0], 2, 4);

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 bottom = Projectile.Bottom;
            Projectile.height = HeightTiles * 16;
            Projectile.Bottom = bottom;
            Projectile.frame = Math.Abs(Projectile.identity * 3 + (int)Projectile.ai[1]) % FrameCount;
            Projectile.frameCounter = Math.Abs(Projectile.identity + (int)Projectile.ai[1]) % TicksPerFrame;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 180);
            target.AddBuff(ModContent.BuffType<CrimsonBurn>(), 180);
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 300);
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (!Main.dedServ && Main.GameUpdateCount % 3 == (ulong)(Projectile.identity % 3))
            {
                bool blood = (Main.GameUpdateCount + (ulong)Projectile.identity) % 3 != 0;
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-6f, 6f), -4f);
                Dust flame = Dust.NewDustPerfect(
                    position,
                    blood ? DustID.Blood : DustID.Wraith,
                    new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-2.2f, -0.8f) * HeightTiles),
                    blood ? 80 : 155,
                    default,
                    blood ? Main.rand.NextFloat(0.55f, 1.05f) : Main.rand.NextFloat(0.45f, 0.75f)
                );
                flame.noGravity = true;
                flame.noLight = !blood;
            }

            Lighting.AddLight(Projectile.Center, 0.35f, 0.02f, 0.03f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new(0, Projectile.frame * frameHeight, texture.Width, frameHeight);

            int age = Lifetime - Projectile.timeLeft;
            float lifetimeFade = MathHelper.Clamp(age / 10f, 0f, 1f)
                * MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);

            DrawRisingTongue(texture, frame, age, 0, lifetimeFade, -1);
            DrawRisingTongue(texture, frame, age, 36, lifetimeFade * 0.8f, 1);
            return false;
        }

        private void DrawRisingTongue(Texture2D texture, Rectangle frame, int age, int phaseOffset, float lifetimeFade, int spinDirection)
        {
            const int RiseCycle = 72;
            float cycle = ((age + phaseOffset + Projectile.identity * 7) % RiseCycle) / (float)RiseCycle;
            float tongueFade = MathF.Sin(cycle * MathHelper.Pi) * lifetimeFade;
            float height = Projectile.height;
            float visualHeight = height * 1.15f;
            float rise = cycle * height * 0.35f;
            float rotation = spinDirection * MathHelper.Lerp(-0.12f, 0.38f, cycle)
                + MathF.Sin(cycle * MathHelper.TwoPi) * 0.08f;
            Vector2 drawPosition = Projectile.Bottom - Main.screenPosition
                + new Vector2(spinDirection * 4f, -rise + 6f);
            Vector2 scale = new Vector2(0.62f, visualHeight / frame.Height);
            SpriteEffects effects = spinDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                Color.White * tongueFade,
                rotation,
                new Vector2(frame.Width * 0.5f, frame.Height),
                scale,
                effects,
                0f
            );
        }
    }

    /// <summary>
    /// Option C: Homing Oolacile Dark Bolt fired during Staff-Axe Weave (hold right-click).
    /// </summary>
    public class GrandOolacileDarkBolt : DynamicTrail
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 180;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            deathSpeed = 1f / 12f;

            trailWidth = 22;
            trailPointLimit = 120;
            trailYOffset = 32;
            trailMaxLength = 140;
            normalCollision = true;
            trailCollision = false;
            NPCSource = false;
            customEffect = ModContent.Request<Effect>(
                "tsorcRevamp/Effects/OolacileBolt",
                ReLogic.Content.AssetRequestMode.ImmediateLoad
            ).Value;
        }

        public override void AI()
        {
            base.AI();

            if (dying)
            {
                return;
            }

            if (Projectile.velocity.LengthSquared() < 225f)
            {
                Projectile.velocity *= 1.02f;
            }

            NPC nearestTarget = null;
            float bestDistanceSq = 480f * 480f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];
                if (!candidate.CanBeChasedBy(this))
                {
                    continue;
                }

                float distSq = Vector2.DistanceSquared(Projectile.Center, candidate.Center);
                if (distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    nearestTarget = candidate;
                }
            }

            if (nearestTarget != null)
            {
                float speed = Projectile.velocity.Length();
                Vector2 desiredDir = (nearestTarget.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 newVel = Vector2.Lerp(Projectile.velocity, desiredDir * speed, 0.08f);
                Projectile.velocity = newVel.SafeNormalize(Vector2.UnitX) * speed;
            }

            Lighting.AddLight(Projectile.Center, 0.55f, 0.08f, 0.25f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 180);
            SpawnBurstDust();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            dying = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            SpawnBurstDust();
            return false;
        }

        public override bool? CanDamage()
        {
            if (dying)
            {
                return false;
            }

            return null;
        }

        private void SpawnBurstDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                int dustType = i % 3 == 0 ? DustID.Shadowflame : DustID.CrimsonSpray;
                Dust burst = Dust.NewDustPerfect(
                    Projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(4.5f, 4.5f),
                    80,
                    default,
                    Main.rand.NextFloat(0.75f, 1.2f)
                );
                burst.noGravity = true;
            }
        }

        private float baseNoiseUOffset;

        public override void SetEffectParameters(Effect effect)
        {
            if (baseNoiseUOffset == 0f)
            {
                baseNoiseUOffset = Main.rand.NextFloat();
            }

            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseUOffset);

            const float pixelBlockSize = 2f;
            Vector2 pixelGridSize = new Vector2(
                Math.Max(trailCurrentLength / pixelBlockSize, 1f),
                Math.Max((trailWidth * 2f) / pixelBlockSize, 1f)
            );
            effect.Parameters["PixelGrid"].SetValue(new Vector4(
                pixelGridSize.X,
                pixelGridSize.Y,
                1f / pixelGridSize.X,
                1f / pixelGridSize.Y
            ));

            visualizeTrail = false;

            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color edgeColor = new Color(1.0f, 0.1f, 0.2f, 1.0f);
            effect.Parameters["slashCenter"].SetValue(Color.Black.ToVector4());
            effect.Parameters["slashEdge"].SetValue(edgeColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }

    /// <summary>
    /// Option B: Dark Judgment shockwave triggered on the 3rd (overhead) left-click swing.
    /// Waits 8 ticks for the overhead slam to descend, then erupts forward with stacked rising
    /// DestinedDeathBlaze flames (like Great Red Knight's wave).
    /// </summary>
    public class GrandOolacileJudgmentWave : ModProjectile
    {
        private const int WindupTicks = 8;
        private bool erupted;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = WindupTicks + 22;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
        }

        public override bool? CanDamage() => erupted ? null : false;
        public override bool PreDraw(ref Color lightColor) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 300);
        }

        public override void AI()
        {
            if (!erupted)
            {
                Player player = Main.player[Projectile.owner];
                if (player.active && !player.dead)
                {
                    Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(player.direction, 0f));
                    Projectile.Center = player.Center + dir * 72f;
                }

                if (Projectile.timeLeft <= 22)
                {
                    erupted = true;
                    Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    Projectile.velocity = dir * 11f;

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.4f }, Projectile.Center);

                        // Same impact sound as the greatsword combo slam (Greatsword.cs GreatswordSwing.SlamSound).
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time") with { Volume = 0.45f }, Projectile.Center);

                        for (int i = 0; i < 36; i++)
                        {
                            bool blood = i % 3 != 0;
                            Vector2 dustVel = dir * Main.rand.NextFloat(2f, 8f)
                                + Main.rand.NextVector2Circular(3.5f, 3.5f);
                            Dust slamDust = Dust.NewDustPerfect(
                                Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                                blood ? DustID.Blood : DustID.Wraith,
                                dustVel,
                                blood ? 80 : 155,
                                default,
                                blood ? Main.rand.NextFloat(0.9f, 1.5f) : Main.rand.NextFloat(0.7f, 1.1f)
                            );
                            slamDust.noGravity = true;
                            slamDust.noLight = !blood;
                        }

                        // Spawn a stacked fan of rising DestinedDeathBlaze sprites on impact.
                        float travelDir = dir.X < 0f ? -1f : 1f;
                        for (int i = 0; i < 6; i++)
                        {
                            float spreadOffset = (i - 2.5f) * 14f;
                            Vector2 spawnPos = Projectile.Center + new Vector2(spreadOffset, 50f);
                            Vector2 riseVel = new Vector2(
                                dir.X * Main.rand.NextFloat(0.8f, 2.4f) + Main.rand.NextFloat(-0.8f, 0.8f),
                                Main.rand.NextFloat(-3.8f, -1.8f)
                            );
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                spawnPos,
                                riseVel,
                                ModContent.ProjectileType<GrandOolacileJudgmentBlaze>(),
                                0,
                                0f,
                                Main.myPlayer,
                                travelDir
                            );
                        }
                    }
                }

                return;
            }

            if (!Main.dedServ)
            {
                if (Projectile.timeLeft % 3 == 0)
                {
                    float travelDir = Projectile.velocity.X < 0f ? -1f : 1f;
                    Vector2 spawnPos = Projectile.Center + new Vector2(Main.rand.NextFloat(-12f, 12f), 48f);
                    Vector2 riseVel = new Vector2(
                        Projectile.velocity.X * 0.18f + Main.rand.NextFloat(-0.5f, 0.5f),
                        Main.rand.NextFloat(-3.2f, -1.5f)
                    );
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPos,
                        riseVel,
                        ModContent.ProjectileType<GrandOolacileJudgmentBlaze>(),
                        0,
                        0f,
                        Main.myPlayer,
                        travelDir
                    );
                }

                for (int i = 0; i < 3; i++)
                {
                    bool crimson = i != 0;
                    Dust waveDust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
                        crimson ? DustID.CrimsonSpray : DustID.Shadowflame,
                        Projectile.velocity * 0.2f + new Vector2(0f, -1.2f),
                        80,
                        default,
                        Main.rand.NextFloat(0.85f, 1.35f)
                    );
                    waveDust.noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, 0.65f, 0.08f, 0.15f);
        }
    }

    /// <summary>
    /// Decorative rising DestinedDeathBlaze sprite for the 3rd-hit Dark Judgment slam and wave,
    /// matching Great Red Knight's lifting blaze style (86x82 x 5 frames).
    /// </summary>
    public class GrandOolacileJudgmentBlaze : ModProjectile
    {
        private const int FrameCount = 5;
        private const int TicksPerFrame = 5;
        private const int Lifetime = 44;

        private int Direction => Projectile.ai[0] < 0f ? -1 : 1;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.frame = Main.rand.Next(FrameCount);
            Projectile.frameCounter = Main.rand.Next(TicksPerFrame);
            Projectile.scale = Main.rand.NextFloat(0.62f, 0.98f);
            Projectile.rotation = Main.rand.NextFloat(-0.18f, 0.18f);
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            Projectile.rotation -= Direction * 0.024f;
            Projectile.velocity *= 0.96f;

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                int dustType = Main.rand.NextBool(2) ? DustID.Blood : DustID.Wraith;
                Vector2 dustPos = Projectile.Bottom + new Vector2(
                    Main.rand.NextFloat(-16f, 16f) * Projectile.scale,
                    Main.rand.NextFloat(-18f, -2f) * Projectile.scale
                );
                Vector2 dustVel = new Vector2(
                    Projectile.velocity.X * 0.2f + Main.rand.NextFloat(-0.5f, 0.5f),
                    Main.rand.NextFloat(-2.6f, -1.0f)
                );
                Dust dust = Dust.NewDustPerfect(dustPos, dustType, dustVel, 100, default, Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.04f, 0.05f) * Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new(0, Projectile.frame * frameHeight, texture.Width, frameHeight);

            float life = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = Utils.Clamp(life * 4.5f, 0f, 1f) * Utils.Clamp((1f - life) * 2.2f, 0f, 1f);

            Vector2 origin = new Vector2(frame.Width * 0.5f, frame.Height);
            SpriteEffects effects = Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White * fade,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0f
            );

            return false;
        }
    }
}
