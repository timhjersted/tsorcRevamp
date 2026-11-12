using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy.SkeletonSet;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Shared base for the bone-pile skeletons (Ice Skeleton, Skeleton Warrior): the Skeleton armor set and the
    /// collapse-and-reassemble death. The first lethal hit drops it into a bone pile with its own small HP pool;
    /// shatter the pile for the real death and loot, or it stands again at 50% HP (subclass-tunable) after 4s. A magic killing blow
    /// (magic damage, or melee under a Magic Weapon buff) skips the pile and kills it outright.
    ///
    /// Subclasses put their per-tick AI in SkeletonAI (skipped while collapsed) and supply the pile values and cues.
    /// Multiplayer: the latch, pile HP, timer, the Pile -> Reform -> stand transitions and the magic-kill flag are
    /// server decisions, written right after the PuppetNPC payload; subclasses write their own fields after base.
    /// </summary>
    public abstract class SkeletonPuppet : PuppetNPC
    {
        // ── Reassembly timing (ticks from the phase start) ──────────────────────────────────────────────────
        //   Pile 240t: AI frozen, contact off, knockback 0; bones fall into the pile over the first 12t; from t180
        //   the bones twitch toward the centre and the eyes flicker (the reform tell). Pile HP 0 = the real death.
        //   Timer out: 30t reform (invulnerable, bones fly together) -> stands -> 60t before it may attack.
        protected const int CollapseTicks = 12;
        protected const int PileTicks = 240;
        protected const int TwitchStart = 180;
        protected const int ReformTicks = 30;
        const int PostReformHoldTicks = 60;

        // The skeleton set's own undyed eye colour (SkeletonEyeGlowLayer), used for the eyes in the pile.
        static readonly Color BoneEyeRed = new Color(255, 48, 32);

        protected enum Reassembly : byte { None, Pile, Reform }

        /// <summary>One bone of the pile, facing right (mirrored when facing left): the gore drawn, its resting
        /// offset from NPC.Bottom and rotation, and the standing offset it falls from (collapse) and flies back to
        /// (reform). The skull carries the eye glow.</summary>
        protected struct PileBone
        {
            public int GoreType;
            public Vector2 RestOffset;
            public float Rotation;
            public Vector2 StandingOffset;
            public bool IsSkull;
        }

        // ── Synced state (written right after the PuppetNPC payload, in this order) ─────────────────────────
        bool _reassemblyUsed;
        Reassembly _reassembly;
        int _reassemblyTimer;
        int _pileFacing = 1;
        // Server: set by HitEffect on a magic killing blow and read by CheckDead in the same StrikeNPC call. It is
        // also written into the death packet, so every client plays the magic-kill burst instead of a pile.
        bool _blessedKillingBlow;

        // ── Server-only ────────────────────────────────────────────────────────────────────────────────────
        int _postReformTicks;

        // ── Per machine: transition tracking ───────────────────────────────────────────────────────────────
        Reassembly _seenReassembly;
        bool _blessedDeathPlayed;

        protected bool Collapsed => _reassembly != Reassembly.None;
        protected Reassembly ReassemblyPhase => _reassembly;
        protected bool ReassemblyUsed => _reassemblyUsed;
        protected int ReassemblyTimer => _reassemblyTimer;
        /// <summary>Server: ticks left of the post-reform hold. Subclasses gate their next attack on it.</summary>
        protected int PostReformTicks => _postReformTicks;

        // ── Armor (dyes stay per subclass) ─────────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<SkeletonHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<SkeletonRibcage>();
        protected override int LegsArmorItemType => ModContent.ItemType<SkeletonLegBones>();

        // ── Per-skeleton values ────────────────────────────────────────────────────────────────────────────
        /// <summary>The pile's HP pool: CheckDead sets life to this instead of letting it die.</summary>
        protected abstract int PileLife { get; }
        /// <summary>Fraction of lifeMax it stands back up with.</summary>
        protected virtual float ReformLifeFraction => 0.5f;
        /// <summary>knockBackResist while standing; the pile and reform force 0.</summary>
        protected abstract float StandingKnockbackResist { get; }
        protected virtual string ReassemblyName => "Reassembly";
        protected virtual Color PileEyeColor => BoneEyeRed;
        /// <summary>Skull-relative eye position for the pile glow, facing left, in unscaled gore pixels.</summary>
        protected virtual Vector2 PileEyeOffset => new Vector2(-4.5f, -1f);
        protected abstract PileBone[] PileBones { get; }

        /// <summary>The standing AI, run every tick except while collapsed. Call RunPuppetAI for the puppet engine.</summary>
        protected abstract void SkeletonAI();

        /// <summary>The PuppetNPC engine tick, for SkeletonAI (AI itself is sealed here so the pile always gates it).</summary>
        protected void RunPuppetAI()
        {
            base.AI();
        }

        /// <summary>Server, in CheckDead, on the first death: clear the subclass's attack state.</summary>
        protected virtual void OnCollapse()
        {
        }

        /// <summary>Every machine, each tick collapsed or reforming. Default: grey bone dust shed by the pile.</summary>
        protected virtual void TickCollapsed()
        {
            if (Main.dedServ)
            {
                return;
            }

            // Bone chips burst off for the first 4t, then dust trickles off the pile while it lies there.
            if (_reassembly == Reassembly.Pile && _reassemblyTimer <= 4)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) + new Vector2(0f, -1.5f);
                    Dust.NewDustPerfect(NPC.Center, DustID.Bone, velocity, 0, default, 1.1f);
                }
            }

            int trickleInterval = 12;
            if (_reassemblyTimer >= TwitchStart)
            {
                trickleInterval = 5;
            }

            if (_reassembly == Reassembly.Pile && _reassemblyTimer % trickleInterval == 0)
            {
                Vector2 spot = NPC.Bottom + new Vector2(Main.rand.NextFloat(-16f, 16f), -6f);
                Dust.NewDustPerfect(spot, DustID.Bone, new Vector2(0f, -0.6f), 0, default, 0.8f);
            }

            // Reform: bone dust drawn inward into the standing frame.
            if (_reassembly == Reassembly.Reform)
            {
                Vector2 from = NPC.Center + Main.rand.NextVector2CircularEdge(36f, 36f);
                Dust pull = Dust.NewDustPerfect(from, DustID.Bone, (NPC.Center - from) * 0.08f, 0, default, 1f);
                pull.noGravity = true;
            }
        }

        /// <summary>Every machine except the dedicated server, on each reassembly transition it sees. Default: the bone
        /// clatter on collapse, a low rattle on reform, and a ring of bone dust when it stands.</summary>
        protected virtual void PlayReassemblyCue(Reassembly from, Reassembly to)
        {
            if (to == Reassembly.Pile)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath2 with { Pitch = -0.2f }, NPC.Center);
            }

            if (to == Reassembly.Reform)
            {
                SoundEngine.PlaySound(SoundID.NPCHit2 with { Pitch = -0.4f }, NPC.Center);
            }

            if (to == Reassembly.None && from == Reassembly.Reform)
            {
                SoundEngine.PlaySound(SoundID.NPCHit2 with { Pitch = -0.2f }, NPC.Center);

                for (int i = 0; i < 24; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(3f, 3f);
                    Dust ring = Dust.NewDustPerfect(NPC.Center, DustID.Bone, velocity, 0, default, 1.2f);
                    ring.noGravity = true;
                }
            }
        }

        /// <summary>Client draw of whatever lies with the bones (weapon, shield), under them.</summary>
        protected virtual void DrawPileProps(SpriteBatch spriteBatch, Vector2 screenPos, Vector2 anchor, int facing, Color light)
        {
        }

        /// <summary>Tints the pile's bones and props. Default: the world light, untinted.</summary>
        protected virtual Color TintPile(Color drawColor)
        {
            return drawColor;
        }

        /// <summary>Client, on the real death (the pile shattered, a second death, or a magic kill): the gores and
        /// burst. fromPile is true when it was the pile that broke.</summary>
        protected abstract void ScatterRealDeath(bool fromPile);

        /// <summary>Debug trace hook for the subclass's own log (server, debug mode).</summary>
        protected virtual void Trace(string kind)
        {
        }

        // ── Per-tick AI ─────────────────────────────────────────────────────────────────────────────────────
        public sealed override void AI()
        {
            // Reassembly phase changes arrive from the server (or from CheckDead here); every machine plays the cues
            // and resets the flags it owns on the transition it sees.
            if (_reassembly != _seenReassembly)
            {
                Reassembly previous = _seenReassembly;
                _seenReassembly = _reassembly;

                if (_reassembly == Reassembly.None && previous == Reassembly.Reform)
                {
                    NPC.dontTakeDamage = false;
                    NPC.knockBackResist = StandingKnockbackResist;
                }

                if (!Main.dedServ)
                {
                    PlayReassemblyCue(previous, _reassembly);
                }
            }

            if (_reassembly != Reassembly.None)
            {
                TickReassembly();
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && _postReformTicks > 0)
            {
                _postReformTicks--;
            }

            SkeletonAI();
        }

        /// <summary>
        /// Every machine, while collapsed or reforming: the standing AI is skipped, velocity braked, knockback and
        /// contact off, invulnerable only while reforming. Clients count the synced timer forward for the draw; the
        /// server owns the Pile -> Reform -> None transitions and the reform health.
        /// </summary>
        void TickReassembly()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _reassemblyTimer++;
            NPC.velocity.X *= 0.8f;
            NPC.knockBackResist = 0f;
            NPC.dontTakeDamage = _reassembly == Reassembly.Reform;
            globalNPC.AttackCommitted = true;
            globalNPC.AttackTelegraphing = false;
            DebugAttackLabel = ReassemblyName + " (" + _reassembly + " t" + _reassemblyTimer + ")";

            TickCollapsed();

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_reassembly == Reassembly.Pile && _reassemblyTimer >= PileTicks)
            {
                _reassembly = Reassembly.Reform;
                _reassemblyTimer = 0;
                NPC.life = Math.Max(1, (int)(NPC.lifeMax * ReformLifeFraction));
                Trace("reassembly=reform");
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
                return;
            }

            if (_reassembly == Reassembly.Reform && _reassemblyTimer >= ReformTicks)
            {
                _reassembly = Reassembly.None;
                _reassemblyTimer = 0;
                _postReformTicks = PostReformHoldTicks;
                Trace("reassembly=stand");
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }
        }

        // ── Death: the pile latch and the magic killing blow ───────────────────────────────────────────────
        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            if (NPC.life > 0)
            {
                return;
            }

            // Magic killing blow (server / singleplayer, the first death only). Vanilla's StrikeNPC runs HitEffect
            // before checkDead, the strike packet carries the damage class, and the server records the hitter in
            // lastInteraction (PlayerInteraction) before striking; buffs are synced player state.
            bool server = Main.netMode != NetmodeID.MultiplayerClient;
            if (server && !_reassemblyUsed && !_blessedKillingBlow)
            {
                bool magicHit = hit.DamageType != null && hit.DamageType.CountsAsClass(DamageClass.Magic);
                bool blessedMeleeHit = false;
                bool meleeHit = hit.DamageType != null && hit.DamageType.CountsAsClass(DamageClass.Melee);
                bool hitterKnown = NPC.lastInteraction >= 0 && NPC.lastInteraction < Main.maxPlayers;

                if (meleeHit && hitterKnown)
                {
                    Player hitter = Main.player[NPC.lastInteraction];
                    blessedMeleeHit = hitter.active && tsorcRevampGlobalNPC.HasMagicWeaponBuff(hitter);
                }

                if (magicHit || blessedMeleeHit)
                {
                    _blessedKillingBlow = true;
                    Trace("blessed-kill");
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            if (_blessedKillingBlow)
            {
                PlayBlessedDeath();
                return;
            }

            // Only the real death scatters bones; the first "death" leaves a pile instead (CheckDead).
            if (_reassemblyUsed)
            {
                ScatterRealDeath(_reassembly == Reassembly.Pile);
            }
        }

        /// <summary>Client: a magic kill. The bones scatter wrapped in Magic Weapon's own swing dust (68 + 15), so the
        /// player sees why it stayed down. Plays once, from HitEffect (singleplayer) or the death packet (clients).</summary>
        void PlayBlessedDeath()
        {
            if (_blessedDeathPlayed)
            {
                return;
            }

            _blessedDeathPlayed = true;
            ScatterRealDeath(false);
            SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.3f, Volume = 0.7f }, NPC.Center);

            for (int i = 0; i < 60; i++)
            {
                int dustType = DustID.BlueCrystalShard;
                if (i % 2 == 0)
                {
                    dustType = DustID.MagicMirror;
                }

                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) + new Vector2(0f, -1f);
                Dust spark = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 0, default, 1.3f);
                spark.noGravity = true;
            }
        }

        public override bool CheckDead()
        {
            if (_reassemblyUsed || _blessedKillingBlow)
            {
                return base.CheckDead();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // A client's local strike reached 0 before the server's answer arrived. Hold at 1 HP so vanilla does
                // not deactivate it here; the server's sync sets the pile's real life, or the real death.
                NPC.life = 1;
                return false;
            }

            // Server latch: refuse the first death. NPC.life is set to the pile's pool rather than left at 0 - an NPC
            // at life 0 before a strike is removed silently, with no OnKill and no loot.
            _reassemblyUsed = true;
            _reassembly = Reassembly.Pile;
            _reassemblyTimer = 0;
            _pileFacing = NPC.direction;
            NPC.life = Math.Max(1, PileLife);
            NPC.velocity.X = 0f;
            StopMeleeHit();
            OnCollapse();
            EnterPhase(AttackPhase.Idle, 0);
            Trace("reassembly=pile");
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
            return false;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return _reassembly == Reassembly.None;
        }

        // ── Bone-pile draw (client) ──────────────────────────────────────────────────────────────────────────
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_reassembly == Reassembly.None)
            {
                return base.PreDraw(spriteBatch, screenPos, drawColor);
            }

            int facing = _pileFacing;
            int timer = _reassemblyTimer;
            Vector2 anchor = NPC.Bottom + new Vector2(0f, NPC.gfxOffY);
            Color light = TintPile(drawColor);
            SpriteEffects flip = SpriteEffects.None;
            if (facing == 1)
            {
                flip = SpriteEffects.FlipHorizontally;
            }

            DrawPileProps(spriteBatch, screenPos, anchor, facing, light);

            // Reform tell: from t180 the bones twitch and creep toward the centre, growing stronger to the reform.
            float twitch = 0f;
            if (_reassembly == Reassembly.Pile && timer >= TwitchStart)
            {
                twitch = (timer - TwitchStart) / (float)(PileTicks - TwitchStart);
            }

            const float BoneScale = 1.1f;
            Vector2 skullPosition = anchor;
            PileBone[] bones = PileBones;

            for (int i = 0; i < bones.Length; i++)
            {
                PileBone bone = bones[i];
                Vector2 rest = new Vector2(bone.RestOffset.X * facing, bone.RestOffset.Y);
                Vector2 standing = new Vector2(bone.StandingOffset.X * facing, bone.StandingOffset.Y);
                Vector2 offset = rest;
                float rotation = bone.Rotation * facing;

                if (_reassembly == Reassembly.Pile && timer < CollapseTicks)
                {
                    // Falling into the pile: accelerating like a drop.
                    float fall = timer / (float)CollapseTicks;
                    offset = Vector2.Lerp(standing, rest, fall * fall);
                    rotation *= fall;
                }
                else if (_reassembly == Reassembly.Pile && twitch > 0f)
                {
                    float jitter = (float)Math.Sin(timer * 1.7f + i * 2.1f) * 1.5f * twitch;
                    offset = Vector2.Lerp(rest, new Vector2(0f, rest.Y), 0.25f * twitch) + new Vector2(jitter, -Math.Abs(jitter));
                    rotation += jitter * 0.08f;
                }
                else if (_reassembly == Reassembly.Reform)
                {
                    // Flying back together into the standing frame.
                    float gather = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(timer / (float)ReformTicks, 0f, 1f));
                    offset = Vector2.Lerp(rest, standing, gather);
                    rotation = MathHelper.Lerp(rotation, 0f, gather);
                }

                // Vanilla gores load on first use; mod gores are always loaded.
                if (bone.GoreType < GoreID.Count)
                {
                    Main.instance.LoadGore(bone.GoreType);
                }

                Texture2D texture = TextureAssets.Gore[bone.GoreType].Value;
                Vector2 position = anchor + offset;
                spriteBatch.Draw(texture, position - screenPos, null, light, rotation, texture.Size() * 0.5f, BoneScale, flip, 0f);

                if (bone.IsSkull)
                {
                    skullPosition = position;
                }
            }

            // Eye glow on the skull: steady and dim in the pile, flickering through the reform tell, flaring on reform.
            float eye = 0.35f;
            if (twitch > 0f)
            {
                eye = 0.35f + 0.65f * twitch * (0.5f + 0.5f * (float)Math.Sin(timer * 0.9f));
            }

            if (_reassembly == Reassembly.Reform)
            {
                eye = 1f;
            }

            Vector2 eyeOffset = PileEyeOffset * BoneScale;
            if (facing == 1)
            {
                eyeOffset.X = -eyeOffset.X;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color eyeColor = PileEyeColor * eye;
            spriteBatch.Draw(pixel, skullPosition + eyeOffset - screenPos, new Rectangle(0, 0, 1, 1), eyeColor, 0f, new Vector2(0.5f), 2.5f, SpriteEffects.None, 0f);
            return false;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_reassembly != Reassembly.None)
            {
                return;
            }

            base.PostDraw(spriteBatch, screenPos, drawColor);
        }

        // ── Multiplayer payload: base first, then the reassembly, then the subclass fields ──────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_reassemblyUsed);
            writer.Write((byte)_reassembly);
            writer.Write(_reassemblyTimer);
            writer.Write(_pileFacing);
            writer.Write(_blessedKillingBlow);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _reassemblyUsed = reader.ReadBoolean();
            _reassembly = (Reassembly)reader.ReadByte();
            _reassemblyTimer = reader.ReadInt32();
            _pileFacing = reader.ReadInt32();
            _blessedKillingBlow = reader.ReadBoolean();

            // The death packet (SyncNPC with life 0) carries the server's magic-kill flag; the client's own strike
            // only held the NPC at 1 HP, so this is where a client learns it died outright.
            if (_blessedKillingBlow && !Main.dedServ)
            {
                PlayBlessedDeath();
            }
        }
    }
}
