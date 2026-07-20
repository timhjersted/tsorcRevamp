using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // A lesser "clutch-crab" summoned by a Quara Pincher's Brood Call (phase 3 of the Pincher rework — see
    // memory project_quara_pincher_rework). It reuses the Pincher's sprite at a smaller scale and lower HP.
    //
    // Role: SUPPORT KITER. It keeps its distance and spits ichor from range (the goo projectile lands in
    // phase 4; for now it's a dust-telegraph stub). It emerges from the ground/water when summoned and, if
    // its parent Pincher dies or vanishes, it burrows back under and despawns — no gore, no souls. Killing
    // one denies the parent that ranged pressure until the parent spends another (interruptible) Brood Call.
    //
    // Never spawns naturally (SpawnChance 0); only Brood Call creates it. Parent index is carried in ai[0].
    public class QuaraClutchCrab : ModNPC, IStaggerable
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/QuaraPincher";

        enum Mode : byte { Emerging = 0, Active, Despawning }

        Mode mode = Mode.Emerging;
        int modeTimer;
        int spitCooldown = 120;
        bool emergeInWater;
        float HoverBob;
        bool statsInitialized;

        const int EmergeTicks = 16;
        const int DespawnTicks = 18;
        const int SpitTelegraphTicks = 22;

        int ParentIndex => (int)NPC.ai[0];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.NPCBestiaryDrawModifiers modifiers = new() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, modifiers);
        }

        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.aiStyle = -1;
            NPC.width = 18;
            NPC.height = 45;
            NPC.scale = 0.85f;
            NPC.damage = 34;
            NPC.defense = 8;
            NPC.lifeMax = 400;       //scaled by tier in InitializeStats
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 0f;          //not a soul source (anti-farm); it's a tactical threat, not a reward
            NPC.npcSlots = 1f;
            NPC.knockBackResist = 0.5f; //overridden by PoiseProfiles
            NPC.alpha = 255;         //starts invisible, fades in during the emerge

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 40;
            g.KiteRangeMin = 9f;     //keeps a spitting distance
            g.KiteRangeMax = 18f;
            g.KiteLooseness = 0.2f;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)mode);
            writer.Write(modeTimer);
            writer.Write(emergeInWater);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            mode = (Mode)reader.ReadByte();
            modeTimer = reader.ReadInt32();
            emergeInWater = reader.ReadBoolean();
        }

        ///<summary>Set by Brood Call right after NPC.NewNPC so the emerge dust reads correctly.</summary>
        public void ConfigureSpawn(int parentIndex, bool inWater)
        {
            NPC.ai[0] = parentIndex;
            emergeInWater = inWater;
            NPC.netUpdate = true;
        }

        Vector2 Maw => NPC.Center + new Vector2(NPC.direction * 12f, -4f);
        bool Submerged => NPC.wet;
        bool ParentAlive => ParentIndex >= 0 && ParentIndex < Main.maxNPCs
            && Main.npc[ParentIndex].active && Main.npc[ParentIndex].type == ModContent.NPCType<QuaraPincher>();

        public void OnStagger(NPC npc)
        {
            //A stagger just interrupts the current spit windup; nothing else to unwind.
            spitCooldown = Math.Max(spitCooldown, 60);
        }

        public override void AI()
        {
            InitializeStats();
            Player player = Main.player[NPC.target];

            //Parent gone -> burrow back under and despawn (unless already leaving).
            if (mode != Mode.Despawning && Main.netMode != NetmodeID.MultiplayerClient && !ParentAlive)
            {
                mode = Mode.Despawning;
                modeTimer = 0;
                NPC.netUpdate = true;
            }

            switch (mode)
            {
                case Mode.Emerging: RunEmerge(); break;
                case Mode.Despawning: RunDespawn(); break;
                default: RunActive(player); break;
            }
        }

        void RunEmerge()
        {
            modeTimer++;
            if (modeTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Pitch = 0.2f }, NPC.Center);
                for (int i = 0; i < 20; i++)
                {
                    int d = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 4f), NPC.width, 8,
                        emergeInWater ? DustID.BubbleBurst_White : DustID.Dirt,
                        Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 100, default, 1.2f);
                    Main.dust[d].noGravity = emergeInWater;
                }
            }
            NPC.alpha = (int)MathHelper.Clamp(255f - modeTimer / (float)EmergeTicks * 255f, 0, 255);
            NPC.velocity.X *= 0.8f;
            if (modeTimer >= EmergeTicks)
            {
                NPC.alpha = 0;
                mode = Mode.Active;
                modeTimer = 0;
            }
        }

        void RunDespawn()
        {
            modeTimer++;
            if (modeTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.4f }, NPC.Center);
            }
            NPC.dontTakeDamage = true; //it's leaving; can't be farmed on the way out
            NPC.alpha = (int)MathHelper.Clamp(modeTimer / (float)DespawnTicks * 255f, 0, 255);
            NPC.velocity.X *= 0.85f;
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 2f), NPC.width, 6,
                    Submerged ? DustID.BubbleBurst_White : DustID.Dirt, 0f, 1f, 120, default, 1.1f);
                Main.dust[d].noGravity = Submerged;
            }
            if (modeTimer >= DespawnTicks)
            {
                NPC.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        void RunActive(Player player)
        {
            //Idle ichor drip identity
            if (Main.rand.NextBool(8))
            {
                int drip = Dust.NewDust(NPC.position, NPC.width, (int)(NPC.height * 0.6f), DustID.Ichor, 0f, 1f, 120, default, 0.6f);
                Main.dust[drip].velocity *= 0.3f;
            }

            //Kite: buoyant hover when wet, otherwise the kiting FighterAI reads the KiteRange levers.
            if (Submerged)
            {
                NPC.noGravity = true;
                HoverBob += 0.1f;
                float dx = player.Center.X - NPC.Center.X;
                int sign = dx >= 0 ? 1 : -1;
                float dist = Math.Abs(dx);
                float desiredX = dist < 9f * 16f ? -sign * 2.2f : dist > 18f * 16f ? sign * 2.2f : sign * 0.5f;
                float targetY = player.Center.Y - 40f + (float)Math.Sin(HoverBob) * 16f;
                float desiredY = MathHelper.Clamp((targetY - NPC.Center.Y) * 0.03f, -2.2f, 2.2f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(desiredX, desiredY), 0.08f);
                NPC.direction = NPC.spriteDirection = sign;
                if (Main.rand.NextBool(2))
                {
                    int bub = Dust.NewDust(NPC.Bottom - new Vector2(NPC.width / 2f, 2f), NPC.width, 6, DustID.BubbleBurst_White, 0f, 1.2f, 120, default, 0.9f);
                    Main.dust[bub].noGravity = true;
                }
            }
            else
            {
                NPC.noGravity = false;
                tsorcRevampAIs.FighterAI(NPC, 1.7f, 0.08f, canTeleport: false, lavaJumping: true, canDodgeroll: false, canPounce: false, canWalkBackwards: true);
            }

            //Ranged spit (stub telegraph until the phase-4 goo projectile). Server-timed.
            if (spitCooldown > 0) spitCooldown--;
            if (spitCooldown <= 0 && !player.dead && player.active && NPC.Distance(player.Center) < 700f)
            {
                RunSpit(player);
            }
        }

        //A short ichor windup at the maw, then a single (weaker) goo glob lobbed at the player.
        void RunSpit(Player player)
        {
            NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
            spitTimer++;
            if (spitTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.5f }, NPC.Center);
            }
            if (spitTimer <= SpitTelegraphTicks)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 from = Maw + Main.rand.NextVector2Circular(20f, 20f);
                    int d = Dust.NewDust(from, 2, 2, DustID.Ichor, 0f, 0f, 120, default, 1f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = (Maw - from) * 0.08f;
                }
            }
            else
            {
                SoundEngine.PlaySound(SoundID.NPCHit20 with { Pitch = 0f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 dir = player.Center - Maw;
                    if (dir != Vector2.Zero) dir.Normalize();
                    Vector2 v = dir * 9f - new Vector2(0f, 3f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), Maw, v,
                        ModContent.ProjectileType<Projectiles.Enemy.QuaraIchorGlob>(), GlobDamage, 1f, Main.myPlayer, 0.18f);
                }
                spitTimer = 0;
                spitCooldown = 150 + Main.rand.Next(60);
            }
        }
        int spitTimer;
        int GlobDamage => tsorcRevampWorld.SuperHardMode ? 30 : Main.hardMode ? 24 : 18;

        void InitializeStats()
        {
            if (statsInitialized) return;
            statsInitialized = true;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int baseHp = 400;
            if (tsorcRevampWorld.SuperHardMode) { baseHp = 1400; NPC.defense = 16; }
            else if (Main.hardMode) { baseHp = 750; NPC.defense = 12; }
            NPC.lifeMax = baseHp;
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }

        public override void OnKill()
        {
            //Player-killed: a burst of ichor + reuse the Pincher's gore at small scale. No item/soul drops.
            for (int i = 0; i < 18; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ichor, 0, 0, 120, default, 1.2f);
            }
            if (Main.netMode != NetmodeID.Server)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("QuaraPincherGore3").Type, 0.9f);
            }
        }
    }
}
