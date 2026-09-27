using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Sheet: ten 94x78 cells, facing right. Closed/open pairs aim 0/22.5/45/67.5/90 degrees UP.
    // Stable root (42,76); no body rotation. See tsorcDocs/CrystalSentryImplementation.md.
    public class CrystalSentry : ModNPC
    {
        private const int Warning = 0, Rise = 1, Idle = 2, Charge = 3, Fire = 4, Recover = 5, Exit = 6;
        private int State { get => (int)NPC.ai[2]; set => NPC.ai[2] = value; }
        private int Timer { get => (int)NPC.ai[3]; set => NPC.ai[3] = value; }
        private Vector2 _root, _aim = Vector2.UnitX;
        private int _remaining = 3600, _angle = 4, _attack, _ownerGeneration;
        private bool _initialized, _hiddenExit;
        private int Owner => (int)NPC.ai[0];
        private float Sink => _hiddenExit ? 76f : State == Rise ? 76f * (1f - MathHelper.SmoothStep(0f, 1f, Math.Min(Timer, 48) / 48f))
            : State == Exit ? 76f * MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((Timer - 16) / 48f, 0f, 1f)) : 0f;
        private float Opacity => State == Rise ? MathHelper.Lerp(0.2f, 1f, Math.Min(Timer, 48) / 48f)
            : State == Exit ? MathHelper.Clamp((64 - Timer) / 24f, 0f, 1f) : 1f;
        private bool OwnerAlive => Owner >= 0 && Owner < Main.maxNPCs && Main.npc[Owner].active
            && Main.npc[Owner].ModNPC is CrystalKnight knight && knight.SentryGeneration == _ownerGeneration;
        private static readonly Vector2[] MouthOffsets = { new(83, 55), new(77, 40), new(72, 36), new(59, 23), new(44, 12) };
        private Vector2 Mouth => _root + new Vector2((MouthOffsets[_angle].X - 42f) * NPC.spriteDirection,
            MouthOffsets[_angle].Y - 76f + Sink);

        public static bool HasOwnedSentry(NPC owner)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].ModNPC is CrystalSentry sentry && sentry.Owner == owner.whoAmI) return true;
            return false;
        }
        public static bool TrySite(NPC owner, Player target, out Vector2 root)
        {
            root = Vector2.Zero;
            int side = Math.Sign(target.Center.X - owner.Center.X);
            if (side == 0) side = owner.direction == 0 ? 1 : owner.direction;
            int preferredX = (int)((target.Center.X + side * 128f) / 16f);
            int footY = (int)(target.Bottom.Y / 16f);
            for (int offset = 0; offset <= 8; offset++)
            {
                int x = preferredX + (offset == 0 ? 0 : ((offset + 1) / 2) * (offset % 2 == 1 ? side : -side));
                for (int step = 0; step <= 8; step++)
                {
                    int y = footY + (step == 0 ? 0 : ((step + 1) / 2) * (step % 2 == 1 ? 1 : -1));
                    Vector2 candidate = new(x * 16f + 8f, y * 16f);
                    float dx = Math.Abs(candidate.X - target.Center.X);
                    if (dx < 96f || dx > 192f || Math.Sign(candidate.X - target.Center.X) != side || !SiteClear(candidate)) continue;
                    Vector2 muzzle = candidate + new Vector2(-side * 41f, -21f);
                    if (target.Center.Y > muzzle.Y + 24f
                        || !Collision.CanHitLine(owner.Center, 1, 1, candidate - new Vector2(0, 40), 1, 1)
                        || !Collision.CanHitLine(muzzle, 1, 1, target.Center, 1, 1)) continue;
                    root = candidate; return true;
                }
            }
            return false;
        }
        private static bool SupportValid(Vector2 root)
        {
            int x = (int)root.X / 16, y = (int)root.Y / 16;
            if (x < 3 || x >= Main.maxTilesX - 3 || y < 7 || y >= Main.maxTilesY - 2) return false;
            for (int i = -1; i <= 1; i++)
            {
                Tile tile = Main.tile[x + i, y];
                if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]
                    || tile.IsHalfBlock || tile.Slope != SlopeType.Solid) return false;
            }
            return true;
        }
        private static bool SiteClear(Vector2 root)
        {
            if (!SupportValid(root) || Collision.SolidCollision(root - new Vector2(54, 80), 108, 80)) return false;
            Rectangle space = new((int)root.X - 54, (int)root.Y - 80, 108, 80);
            for (int x = space.Left / 16; x <= space.Right / 16; x++)
                for (int y = space.Top / 16; y < space.Bottom / 16; y++)
                    if (Main.tile[x, y].LiquidAmount > 0 && Main.tile[x, y].LiquidType == LiquidID.Lava) return false;
            space.Inflate(32, 16);
            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i].active && !Main.player[i].dead && space.Intersects(Main.player[i].Hitbox)) return false;
            return true;
        }
        public static bool Spawn(NPC owner, Player target, int sequence, int warning)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || HasOwnedSentry(owner) || !TrySite(owner, target, out Vector2 root)) return false;
            int index = NPC.NewNPC(owner.GetSource_FromAI(), (int)root.X, (int)root.Y,
                ModContent.NPCType<CrystalSentry>(), 0, owner.whoAmI, sequence);
            if (index >= Main.maxNPCs) return false;
            var sentry = (CrystalSentry)Main.npc[index].ModNPC;
            sentry._root = root; sentry._ownerGeneration = sequence; sentry._initialized = true;
            sentry.Timer = -warning; sentry.NPC.target = target.whoAmI;
            sentry.NPC.direction = sentry.NPC.spriteDirection = target.Center.X >= root.X ? 1 : -1;
            sentry.NPC.Bottom = root; sentry.NPC.life = sentry.NPC.lifeMax = 4000; sentry.NPC.netUpdate = true;
            return true;
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 10;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frozen] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Chilled] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }
        public override void SetDefaults()
        {
            // Cover the horizontal head too, so ranged/melee hits on it can destroy the sentry.
            NPC.width = 104; NPC.height = 76; NPC.lifeMax = 4000; NPC.defense = 20; NPC.damage = 0;
            NPC.knockBackResist = 0; NPC.value = 0; NPC.npcSlots = 0;
            NPC.noGravity = true; NPC.noTileCollide = true; NPC.lavaImmune = true; NPC.aiStyle = -1;
            NPC.HitSound = SoundID.Item27; NPC.DeathSound = SoundID.Item27;
            NPC.dontTakeDamage = true;
        }
        // User requested 4000 actual HP, including Expert/Master (not a scaled base value).
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) => NPC.lifeMax = 4000;
        public override bool CheckActive() => false;
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
            => State == Warning || State == Exit ? false : null;
        private void Enter(int state)
        {
            if (state == Exit && State == Warning) _hiddenExit = true;
            State = state; Timer = 0; NPC.netUpdate = true;
        }
        public override void AI()
        {
            if (!_initialized)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                // Developer/manual spawns have no owning cast: withdraw safely.
                _root = NPC.Bottom; _initialized = true; Enter(Exit);
            }
            NPC.Bottom = _root; NPC.velocity = Vector2.Zero; NPC.timeLeft = 120;
            NPC.dontTakeDamage = State == Warning || State == Exit || State == Rise && Timer < 48;
            Timer++;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority)
            {
                if (State != Exit && (!OwnerAlive || !SupportValid(_root))) Enter(Exit);
                if (State >= Idle && State <= Recover && --_remaining <= 0) Enter(Exit);
                if (State == Warning)
                {
                    if (!OwnerAlive || !((CrystalKnight)Main.npc[Owner].ModNPC).HasLiveCast((int)NPC.ai[1])) Enter(Exit);
                    else if (Timer >= 0)
                    {
                        if (!SiteClear(_root)) Enter(Exit);
                        else Enter(Rise);
                    }
                }
                else if (State == Rise && Timer >= 72) Enter(Idle);
                else if (State == Idle)
                {
                    NPC.TargetClosest(false);
                    if (NPC.HasValidTarget)
                    {
                        Player target = Main.player[NPC.target];
                        bool ready = AimAt(target);
                        float distance = Vector2.Distance(Mouth, target.Center);
                        bool line = Collision.CanHitLine(Mouth, 1, 1, target.Center, 1, 1);
                        if (ready && Timer >= 30 && line && (distance <= 160f || distance >= 224f && distance <= 720f))
                        { _attack = distance <= 160f ? 0 : 1; Enter(Charge); }
                    }
                }
                else if (State == Charge)
                {
                    if (!NPC.HasValidTarget) Enter(Recover);
                    else
                    {
                        if (Timer < 28) AimAt(Main.player[NPC.target]);
                        if (Timer == 28) NPC.netUpdate = true;
                        if (Timer >= 40)
                        {
                            if (_aim.X * NPC.spriteDirection < 0 || Collision.SolidCollision(Mouth - new Vector2(6), 12, 12)) Enter(Recover);
                            else { Enter(Fire); if (_attack == 1) ShootCrystal(); }
                        }
                    }
                }
                else if (State == Fire)
                {
                    if (_attack == 0 && Timer <= 42 && (Timer - 1) % 3 == 0)
                    {
                        Vector2 velocity = _aim.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-10, 10))) * 8f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), Mouth, velocity,
                            ModContent.ProjectileType<CrystalSentryBreath>(), 20, 1, Main.myPlayer, NPC.whoAmI, _ownerGeneration);
                    }
                    if (Timer >= (_attack == 0 ? 42 : 6)) Enter(Recover);
                }
                else if (State == Recover && Timer >= (_attack == 0 ? 90 : 60)) Enter(Idle);
                else if (State == Exit && Timer >= 64)
                {
                    // Natural withdrawal is not destruction: no OnKill shards or loot.
                    NPC.active = false; NPC.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
                if (Main.GameUpdateCount % 60 == 0) NPC.netUpdate = true;
            }
            if (State == Rise) _angle = Timer < 48 ? 4 : Math.Max(0, 4 - (Timer - 48) / 4);
            if (State == Exit && Timer % 4 == 0) _angle = Math.Min(4, _angle + 1);
            if (!Main.dedServ) Decorate();
        }
        private bool AimAt(Player target)
        {
            if (Timer % 4 != 0) return false;
            int face = target.Center.X >= _root.X ? 1 : -1;
            if (face != NPC.spriteDirection)
            {
                if (_angle < 4) _angle++;
                else NPC.direction = NPC.spriteDirection = face;
                NPC.netUpdate = true; return false;
            }
            Vector2 desired = (target.Center - Mouth).SafeNormalize(new Vector2(face, 0));
            float elevation = MathF.Atan2(-desired.Y, Math.Abs(desired.X));
            if (elevation < -0.15f) return false;
            int bucket = (int)MathF.Round(MathHelper.Clamp(elevation, 0, MathHelper.PiOver2) / (MathHelper.Pi / 8f));
            if (Math.Abs(elevation - _angle * MathHelper.Pi / 8f) > MathHelper.Pi / 16f + MathHelper.ToRadians(4))
                _angle += Math.Sign(bucket - _angle);
            _aim = (target.Center - Mouth).SafeNormalize(new Vector2(face, 0));
            NPC.netUpdate = true;
            // The hysteresis band is a valid aiming pose too; do not leave an idle dead zone at a boundary.
            return Math.Abs(elevation - _angle * MathHelper.Pi / 8f) <= MathHelper.Pi / 16f + MathHelper.ToRadians(4);
        }
        private void ShootCrystal() => Projectile.NewProjectile(NPC.GetSource_FromAI(), Mouth, _aim * 8f,
            ModContent.ProjectileType<CrystalSentryCrystal>(), 28, 2, Main.myPlayer);
        private void Decorate()
        {
            if (State == Warning)
            {
                float progress = MathHelper.Clamp(1f + Timer / 30f, 0, 1);
                for (int i = -2; i <= 2; i++)
                    CrystalKnightShard.FrostDust(_root + new Vector2(i * 10, -2), new Vector2(0, -0.4f), 0.65f + progress * 0.4f);
                return;
            }
            if ((State == Rise && Timer == 1) || (State == Exit && Timer == 1))
            {
                CrystalKnightShard.Shatter(_root - new Vector2(0, 6), 70);
                SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.3f }, _root);
            }
            if (State == Rise || State == Exit)
                for (int i = 0; i < 5; i++)
                    CrystalKnightShard.FrostDust(_root + new Vector2(Main.rand.NextFloat(-26, 26), -2),
                        new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2.5f, -0.5f)), Main.rand.NextFloat(0.7f, 1.5f));
            if (State == Exit && Timer == 60) CrystalKnightShard.Shatter(_root, 70);
            if (State == Charge)
            {
                float progress = Timer / 40f;
                CrystalKnightShard.GatherDust(Mouth, progress, _attack == 0 ? 26 : 20);
                CrystalKnightShard.GatherDust(Mouth, progress, 18);
                if (Timer % 4 == 0)
                    for (int i = 1; i <= 6; i++) CrystalKnightShard.FrostDust(Mouth + _aim * (i * 12), Vector2.Zero, 0.5f);
                if (Timer == 1) SoundEngine.PlaySound(SoundID.Item28 with { Pitch = -0.4f, Volume = 0.6f }, Mouth);
            }
            if (State == Fire && Timer == 1) SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f }, Mouth);
            if (Main.GameUpdateCount % 8 == 0) CrystalKnightShard.FrostDust(Mouth, new Vector2(0, -0.5f), 0.6f);
            Lighting.AddLight(Mouth, new Vector3(0.1f, 0.3f, 0.4f) * Opacity);
        }
        public bool BreathActive(int generation) => _ownerGeneration == generation && State != Exit && State != Warning;
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ) return;
            if (NPC.life > 0) { CrystalKnightShard.Shatter(NPC.Center, 6); return; }
            // Three layers: fast ice chips, medium cold body, slow expanding frost cloud.
            for (int i = 0; i < 180; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                int type = i < 100 ? DustID.IceTorch : i < 150 ? DustID.BlueCrystalShard : DustID.Frost;
                Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(24, 32), type,
                    direction * Main.rand.NextFloat(i < 100 ? 3 : 1, i < 150 ? 10 : 3), 50, Color.LightCyan,
                    Main.rand.NextFloat(i < 150 ? 0.7f : 0.5f, i < 150 ? 1.6f : 0.8f));
                dust.noGravity = i < 100 || i >= 150;
                if (i >= 150) dust.fadeIn = 1.5f;
            }
        }
        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // Deliberate damaging destruction burst requested by the user. All 12 form for20t first.
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 5.5f;
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center + velocity * 3,
                    velocity, ModContent.ProjectileType<CrystalSentryCrystal>(), 30, 2, Main.myPlayer, 1);
            }
        }
        public override void FindFrame(int frameHeight)
        {
            bool open = State == Fire || State == Charge && Timer >= 32;
            NPC.frame = new Rectangle(0, (_angle * 2 + (open ? 1 : 0)) * 78, 94, 78);
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!_initialized) return false;
            if (State == Warning)
            {
                // Solid marker survives dust budget throttling; separated segments read as a floor glyph.
                for (int i = -2; i <= 2; i++) spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(_root.X - screenPos.X) + i * 10 - 3, (int)(_root.Y - screenPos.Y) - 3, 6, 2), Color.LightCyan * 0.7f);
                return false;
            }
            Rectangle source = NPC.frame;
            int visibleHeight = Math.Min(78, Math.Max(0, (int)(76f - Sink)));
            if (visibleHeight <= 0) return false;
            source.Height = visibleHeight;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 position = _root + new Vector2(0, Sink) - screenPos;
            Vector2 origin = new(NPC.spriteDirection < 0 ? 52 : 42, 76);
            spriteBatch.Draw(TextureAssets.Npc[Type].Value, position, source,
                Color.Lerp(drawColor, Color.LightCyan, 0.45f) * Opacity, 0, origin, 1, effects, 0);
            if (State == Charge && _attack == 1)
            {
                Texture2D crystal = ModContent.Request<Texture2D>("tsorcRevamp/Content/Projectiles/Enemy/EnemyCrystalKnightBolt").Value;
                spriteBatch.Draw(crystal, Mouth - screenPos, null, Color.LightCyan * (Timer / 40f),
                    Timer * 0.06f, crystal.Size() / 2, MathHelper.Lerp(0.25f, 1, Timer / 40f), SpriteEffects.None, 0);
            }
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_initialized); writer.Write(_hiddenExit); writer.Write(_root.X); writer.Write(_root.Y); writer.Write(_ownerGeneration);
            writer.Write(_remaining); writer.Write((byte)_angle); writer.Write((byte)_attack);
            writer.Write(_aim.X); writer.Write(_aim.Y); writer.Write((sbyte)NPC.spriteDirection);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _initialized = reader.ReadBoolean(); _hiddenExit = reader.ReadBoolean(); _root = new(reader.ReadSingle(), reader.ReadSingle()); _ownerGeneration = reader.ReadInt32();
            _remaining = reader.ReadInt32(); _angle = reader.ReadByte(); _attack = reader.ReadByte();
            _aim = new(reader.ReadSingle(), reader.ReadSingle()); NPC.direction = NPC.spriteDirection = reader.ReadSByte();
        }
    }
}
