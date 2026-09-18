using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    /// <summary>
    /// Artorias's spectral phantom: a real, invulnerable, melee-only Artorias that the boss summons below 50%
    /// health (Artorias.TickSpectralPhantom). Same SmartFighter4 navigator, loadout, combo table and sword
    /// crescents as Artorias (ArtoriasSwordsman), swung faster and pressed harder; it fades out after
    /// PhantomComboCount combos. Replaces the scripted Umbral Echo Step afterimage.
    /// NPC.ai[0] = the owning Artorias's whoAmI.
    /// </summary>
    class ArtoriasPhantom : ArtoriasSwordsman
    {
        // ── Lifecycle ────────────────────────────────────────────────────────────
        const int ArrivalTicks = 36;          // fades in with dust, standing, before it may act
        const int ExitTicks = 40;             // fades out with dust, standing, then is removed
        const int PhantomComboCount = 2;      // combos it starts before leaving
        const int MaxLifetimeTicks = 720;     // leaves anyway if it never manages to reach the player
        const float PhantomOpacity = 0.85f;

        int _lifeTicks;
        int _exitTimer = -1;                  // -1 = still fighting; counts ExitTicks down to 0
        int _combosStarted;

        /// <summary>0..1 visibility: ramps in over ArrivalTicks, back out over ExitTicks.</summary>
        float VisibleFraction
        {
            get
            {
                float arrival = MathHelper.Clamp(_lifeTicks / (float)ArrivalTicks, 0f, 1f);
                float exit = 1f;
                if (_exitTimer >= 0)
                {
                    exit = MathHelper.Clamp(_exitTimer / (float)ExitTicks, 0f, 1f);
                }

                return arrival * exit;
            }
        }

        // ── Not an encounter of its own ──────────────────────────────────────────
        protected override bool AnnounceInvasion => false;
        protected override bool DespawnsOnPartyWipe => false;
        // No attack picks while materializing, fading, or once its combos are spent.
        protected override bool HoldAttackSelection => _lifeTicks < ArrivalTicks || _exitTimer >= 0
            || _combosStarted >= PhantomComboCount;

        // ── Aggression ───────────────────────────────────────────────────────────
        // 0.65 x Artorias's 55: the share the old Echo Step strike dealt.
        protected override int MeleeDamage => 36;
        // Runs from 140px out (base 420) and sprints harder while closing to engage range. The base combo
        // gate still refuses to swing beyond MeleeEngageRange, so it closes the gap first, jumping as needed.
        protected override float RunDistance => 140f;
        protected override float ClosingDistanceSpeedMult => 1.6f;
        protected override int ClosingDistanceMaxTicks => 240;
        // Never strolls, and starts a combo on the first eligible Idle tick instead of a 65% per-tick roll.
        protected override int MeleeComboChance => 100;
        protected override int CasualStrollChance => 0;
        // 1.4x swing clock, recovery and pauses / 1.4; tells play at their authored length, floor 20t.
        protected override float ComboTempoMult => 1.4f;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;

        // ── Spectral look ────────────────────────────────────────────────────────
        // The overlay rewrites the whole finished player draw cache, so the greatsword gets exactly the
        // same tint, halo and trail as the body. Scale 1 keeps hand position and blade reach unchanged.
        protected override bool HasSpectralOverlay => true;
        protected override float SpectralOverlayScale => 1f;
        protected override Color SpectralOverlayColor => new Color(170, 74, 238);
        protected override float SpectralCoreTintStrength => 0.55f;
        protected override float SpectralCoreOpacity => PhantomOpacity * VisibleFraction;
        protected override Color SpectralHaloColor => new Color(120, 40, 200);
        protected override int SpectralHaloCopyCount => 8;
        protected override float SpectralHaloRadius => 4f;
        protected override float SpectralHaloOpacity => 0.3f * VisibleFraction;
        protected override float SpectralHaloScale => 1f;
        protected override bool SpectralHaloFollowsCoreOpacity => true;
        protected override float SpectralTrailOpacity => 0.35f * VisibleFraction;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.ImmuneToAllBuffs[Type] = true;
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryHidden = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryHidden);
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.width = 30;
            NPC.height = 40;
            NPC.lifeMax = 1000;
            NPC.damage = 0; // all damage via weapon hitboxes, like Artorias
            NPC.defense = 0;
            NPC.knockBackResist = 0f;
            // Invulnerable threat. dontTakeDamage also keeps homing weapons and minions from chasing it.
            NPC.dontTakeDamage = true;
            NPC.value = 0f;
            NPC.npcSlots = 0f;
            NPC.lavaImmune = true;

            tsorcRevampGlobalNPC phantomGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            phantomGlobalNPC.NavSearchRadius = 80; // Artorias's own navigator reach
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void AI()
        {
            int ownerIndex = (int)NPC.ai[0];
            bool ownerAlive = ownerIndex >= 0 && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].type == ModContent.NPCType<Artorias>();

            // Press the same player Artorias is fighting.
            if (ownerAlive)
            {
                NPC.target = Main.npc[ownerIndex].target;
            }

            // The server decides when to leave: at once if Artorias is gone, otherwise once its combos are spent
            // (or it timed out) and it is between attacks, so it never vanishes mid-swing.
            if (Main.netMode != NetmodeID.MultiplayerClient && _exitTimer < 0)
            {
                bool betweenAttacks = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll
                    || Phase == AttackPhase.ClosingDistance;
                bool doneFighting = _combosStarted >= PhantomComboCount || _lifeTicks >= MaxLifetimeTicks;

                if (!ownerAlive || (doneFighting && betweenAttacks))
                {
                    _exitTimer = ExitTicks;
                    NPC.netUpdate = true;
                }
            }

            if (_lifeTicks == 0 && !Main.dedServ)
            {
                SpawnPhantomDustBurst(materializing: true);
            }

            base.AI();
            _lifeTicks++;

            // Ambient abyss motes and a violet light, both scaled by how materialized it is.
            float visible = VisibleFraction;
            if (!Main.dedServ && visible > 0.05f)
            {
                Lighting.AddLight(NPC.Center, new Vector3(0.36f, 0.12f, 0.56f) * visible);
                if (Main.rand.NextBool(3))
                {
                    Vector2 offset = Main.rand.NextVector2Circular(22f, 30f);
                    int dustType = DustID.PurpleTorch;
                    if (Main.rand.NextBool(5))
                    {
                        dustType = DustID.ShadowbeamStaff;
                    }

                    Dust dust = Dust.NewDustPerfect(NPC.Center + offset, dustType, -offset * 0.06f, 110,
                        new Color(170, 74, 238), 0.8f * visible);
                    dust.noGravity = true;
                }
            }

            if (_exitTimer > 0)
            {
                _exitTimer--;
                if (_exitTimer == 0 && !Main.dedServ)
                {
                    SpawnPhantomDustBurst(materializing: false);
                }
            }

            // Fully faded: the server removes it and tells clients. A client holds at 0 (invisible) until then.
            if (_exitTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
            }
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _combosStarted++;
                NPC.netUpdate = true;
            }
        }

        /// <summary>Arrival: motes converge inward. Exit: they burst outward. Every 7th mote is a silver
        /// flash and every 3rd a shadowbeam, the same material as Artorias's other abyss effects.</summary>
        void SpawnPhantomDustBurst(bool materializing)
        {
            const int MoteCount = 24;
            for (int i = 0; i < MoteCount; i++)
            {
                Vector2 radial = (MathHelper.TwoPi * i / MoteCount + Main.rand.NextFloat(-0.16f, 0.16f))
                    .ToRotationVector2();
                float distance = Main.rand.NextFloat(8f, 28f);
                Vector2 velocity = radial * Main.rand.NextFloat(1.3f, 3.4f);
                if (materializing)
                {
                    distance = Main.rand.NextFloat(30f, 46f);
                    velocity = -radial * Main.rand.NextFloat(0.9f, 1.8f);
                }

                int dustType = DustID.PurpleTorch;
                if (i % 7 == 0)
                {
                    dustType = DustID.SilverFlame;
                }
                else if (i % 3 == 0)
                {
                    dustType = DustID.ShadowbeamStaff;
                }

                Dust dust = Dust.NewDustPerfect(NPC.Center + radial * distance, dustType, velocity, 105,
                    new Color(176, 72, 242), Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Abyss mantle behind the body, the same material Artorias wears for his big casts; it swells
            // while the phantom is swinging.
            float visible = VisibleFraction;
            if (visible > 0.01f)
            {
                float mantleOpacity = 0.38f;
                float mantleIntensity = 0.88f;
                if (Phase == AttackPhase.MeleeComboAttack || Phase == AttackPhase.MeleeAttack)
                {
                    mantleOpacity = 0.52f;
                    mantleIntensity = 1.2f;
                }

                ArtoriasVFX.DrawMantle(NPC.Center + new Vector2(0f, -12f),
                    new Vector2(148f, 190f), mantleOpacity * visible, mantleIntensity, -1f);
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((short)_exitTimer);
            writer.Write((byte)Math.Min(_combosStarted, byte.MaxValue));
            writer.Write((short)Math.Min(_lifeTicks, short.MaxValue));
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _exitTimer = reader.ReadInt16();
            _combosStarted = reader.ReadByte();
            _lifeTicks = reader.ReadInt16();
        }
    }
}
