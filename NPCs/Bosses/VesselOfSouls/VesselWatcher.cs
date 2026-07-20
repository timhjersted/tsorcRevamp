using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.VesselOfSouls
{
    ///<summary>
    ///A disembodied Strange Eye conjured by the Vessel of Souls. Two roles, chosen by ai[2]:
    ///  Servant (0) — phase 1 (Spawn the Watchers). Fans out around the player at a 10–20 tile KITE
    ///                distance (upper hemisphere + sides, never the bottom), positions from its live RANK
    ///                so survivors re-spread when one dies. Fan slowly sways (faster below 50%/25% HP),
    ///                each eye floats + repels the others, moves slowly (catchable). SAFE-LANE fire: only
    ///                alternating ranks fire each volley, so a gap always exists. Backs off to ≥5 tiles
    ///                before firing; the closer it is, the slower the skull. No contact damage.
    ///  Sentinel (1) — phase 2 (The Watching Wall). Eases to an arena-rim anchor (ai[0],ai[1]) and holds.
    ///  ai[0]=anchorX  ai[1]=anchorY  ai[2]=mode(0 servant /1 sentinel)  ai[3]=slot(servant) | fireOffset(sentinel)
    ///</summary>
    class VesselWatcher : ModNPC
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/VesselOfSouls/StrangeEye";

        // If the gaze looks the wrong way, bump this (the sprite's frame-0 facing may differ).
        const int GazeFrameOffset = 0;

        const float KiteRadius = 240f;        // ~15 tiles — middle of the 10–20 tile band
        const float MinFireDist = 80f;        // ≥5 tiles before it may fire
        const int ServantFire = 110;          // fire cadence (servant) — +30 ticks so shots aren't spammy
        const int SentinelFire = 90;
        const int DetonateWindup = 150;       // 2.5s dust buildup before a timed-out watcher blows
        const float ServantSpeedCap = 3.6f;   // LOW → the player can chase one down and melee it

        int Mode => (int)NPC.ai[2];
        int Slot => (int)NPC.ai[3];
        int FireOffset => (int)NPC.ai[3];
        Vector2 Anchor => new Vector2(NPC.ai[0], NPC.ai[1]);

        int fireTimer;   // sentinels
        int age;
        float orbitPhase;
        int SkullDamage = 12; // hostile → 24 on hit

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 9;
            NPCID.Sets.MPAllowedEnemies[NPC.type] = true;
            NPCID.Sets.TrailCacheLength[NPC.type] = 8;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 0;   // no contact damage — only its skulls hurt
            NPC.defense = 0;
            NPC.lifeMax = 50;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.npcSlots = 0.5f;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Servants belong to phase 1; sentinels belong to phase 2. Assign after ai[] has been
            // populated so the two forms have their requested exact health values.
            NPC.lifeMax = Mode == 1 ? 25 : 50;
            NPC.life = NPC.lifeMax;
            fireTimer = Mode == 1 ? FireOffset : 0;
            orbitPhase = Slot * 0.8f; // desync the wander per eye
        }

        public override void AI()
        {
            age++;
            bool bossAlive = NPC.AnyNPCs(ModContent.NPCType<VesselOfSouls>());
            int despawnAge = Mode == 1 ? 360 : 1200;
            if (!bossAlive) { DisperseAndDie(); return; }        // boss gone → quiet disperse (no blast)
            if (age >= despawnAge) { Detonate(); return; }        // timed out → detonate
            bool charging = age >= despawnAge - DetonateWindup;   // last 2.5s: telegraphed dust buildup
            if (charging) DetonateCharge(despawnAge - age);

            NPC.TargetClosest(false);
            Player player = Main.player[NPC.target];
            if (!player.active || player.dead) { NPC.velocity *= 0.95f; return; }
            float dist = Vector2.Distance(NPC.Center, player.Center);
            bool los = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

            if (Mode == 1)
            {
                Vector2 to = Anchor - NPC.Center;
                Vector2 seek = to.Length() > 4f ? to.SafeNormalize(Vector2.Zero) * MathHelper.Min(to.Length() * 0.06f, 9f) : Vector2.Zero;
                NPC.velocity = Vector2.Lerp(NPC.velocity, seek, 0.08f);
                if (!charging) SentinelFireLogic(player, dist, los);
            }
            else
            {
                ServantLogic(player, dist, los, charging);
            }

            if (!charging) Lighting.AddLight(NPC.Center, 0.4f, 0.1f, 0.5f);
        }

        void ServantLogic(Player player, float dist, bool los, bool charging)
        {
            orbitPhase += 0.02f * OrbitSpeedFactor();
            ComputeRank(out int alive, out int rank);

            // SAFE-LANE: on each volley window, only ranks whose parity matches fire — always a gap.
            int window = (int)(Main.GameUpdateCount / ServantFire);
            int windowPos = (int)(Main.GameUpdateCount % ServantFire);
            bool myTurn = alive <= 1 || (rank % 2) == (window % 2);
            int ticksToFire = (ServantFire - 1) - windowPos;
            float converge = (!charging && myTurn && ticksToFire >= 0 && ticksToFire < 20) ? (20 - ticksToFire) / 20f : 0f;

            // Movement: ease toward this eye's flight-pattern point (slow), repel other eyes, back off if
            // inside 5 tiles. Each rank flies a DIFFERENT pattern (arc / figure-8 / upper circle).
            Vector2 desired = ServantDesired(player, alive, rank, converge);
            Vector2 seek = (desired - NPC.Center) * 0.06f;
            if (seek.Length() > ServantSpeedCap) seek = seek.SafeNormalize(Vector2.Zero) * ServantSpeedCap;
            seek += Separation();
            if (dist < MinFireDist) seek += (NPC.Center - player.Center).SafeNormalize(Vector2.UnitY) * 3.5f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, seek, 0.06f);

            if (charging) return; // charging to detonate — no shots

            bool canFire = myTurn && los && dist >= MinFireDist;
            if (canFire && windowPos >= ServantFire - 18 && !Main.dedServ) TelegraphTick();
            if (canFire && windowPos == ServantFire - 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float speed = MathHelper.Lerp(3.2f, 6.5f, MathHelper.Clamp((dist - MinFireDist) / 320f, 0f, 1f)); // slower up close
                FireSkull(player, speed);
            }
        }

        void SentinelFireLogic(Player player, float dist, bool los)
        {
            fireTimer++;
            if (fireTimer == SentinelFire - 18 && los && !Main.dedServ) TelegraphTick();
            if (fireTimer >= SentinelFire && los && Main.netMode != NetmodeID.MultiplayerClient)
            {
                fireTimer = 0;
                FireSkull(player, MathHelper.Lerp(4f, 7f, MathHelper.Clamp((dist - MinFireDist) / 320f, 0f, 1f)));
            }
        }

        void FireSkull(Player player, float speed)
        {
            Vector2 vel = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * speed;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), SkullDamage, 1f, Main.myPlayer, 0f);
            NPC.netUpdate = true;
        }

        float OrbitSpeedFactor()
        {
            int b = NPC.FindFirstNPC(ModContent.NPCType<VesselOfSouls>());
            if (b < 0) return 1f;
            float frac = Main.npc[b].life / (float)Main.npc[b].lifeMax;
            return frac <= 0.25f ? 1.6f : (frac <= 0.5f ? 1.3f : 1f);
        }

        void ComputeRank(out int alive, out int rank)
        {
            alive = 0; rank = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.type == NPC.type && (int)n.ai[2] == 0)
                {
                    alive++;
                    if (i < NPC.whoAmI) rank++;
                }
            }
        }

        // Each servant flies a DIFFERENT pattern by rank so a group never reads as one rigid ring. All
        // patterns stay in the upper hemisphere (never the floor), at kite distance, + an individual
        // wander, then get pulled out of tiles / into LOS.
        Vector2 ServantDesired(Player player, int alive, int rank, float converge)
        {
            float radius = KiteRadius - converge * 45f;
            float t = orbitPhase;
            Vector2 offset;
            switch (rank % 3)
            {
                default: // 0 — swaying arc slot (spread across the group)
                {
                    float spread = alive <= 1 ? 0f : MathHelper.Lerp(-1.3f, 1.3f, rank / (float)(alive - 1));
                    float bearing = spread + (float)Math.Sin(t) * 0.5f;
                    offset = new Vector2((float)Math.Sin(bearing), -(float)Math.Cos(bearing)) * radius;
                    break;
                }
                case 1: // horizontal figure-8 above the player
                    offset = new Vector2((float)Math.Sin(t) * radius, -(0.55f + 0.35f * (float)Math.Abs(Math.Sin(t * 2f))) * radius);
                    break;
                case 2: // circle that stays in the upper hemisphere
                    offset = new Vector2((float)Math.Cos(t) * radius, -(0.5f + 0.5f * (float)Math.Abs(Math.Sin(t))) * radius);
                    break;
            }
            Vector2 pt = player.Center + offset;

            float wph = NPC.whoAmI * 1.7f;
            pt += new Vector2((float)Math.Sin(t * 2.3f + wph) * 18f, (float)Math.Cos(t * 1.9f + wph) * 14f);

            for (int i = 0; i < 10; i++)
            {
                bool solid = Collision.SolidCollision(pt - new Vector2(NPC.width / 2f, NPC.height / 2f), NPC.width, NPC.height);
                bool los = Collision.CanHitLine(pt, 1, 1, player.Center, 1, 1);
                if (!solid && los) break;
                pt = Vector2.Lerp(pt, player.Center, 0.14f);
            }
            return pt;
        }

        Vector2 Separation()
        {
            Vector2 push = Vector2.Zero;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (i == NPC.whoAmI || !n.active || n.type != NPC.type) continue;
                Vector2 d = NPC.Center - n.Center;
                float dd = d.Length();
                if (dd > 0.5f && dd < 80f) push += d.SafeNormalize(Vector2.Zero) * (1f - dd / 80f) * 2.5f;
            }
            if (push.Length() > 3f) push = push.SafeNormalize(Vector2.Zero) * 3f;
            return push;
        }

        // Bright, converging purple telegraph so the incoming shot is easy to read.
        void TelegraphTick()
        {
            for (int i = 0; i < 3; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 from = NPC.Center + ang.ToRotationVector2() * Main.rand.NextFloat(28f, 46f);
                int d = Dust.NewDust(from, 4, 4, i == 0 ? DustID.Shadowflame : DustID.PurpleTorch, 0f, 0f, 80, default, Main.rand.NextFloat(1.4f, 2f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = (NPC.Center - from) * 0.15f;
                Main.dust[d].fadeIn = 0.4f;
            }
            Lighting.AddLight(NPC.Center, 0.7f, 0.15f, 0.9f);
        }

        // Boss gone: a quiet ethereal disperse (no blast).
        void DisperseAndDie()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame, 0f, 0f, 100, default, 1.2f);
                    Main.dust[d].noGravity = true;
                }
            }
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
        }

        // The last 2.5s of a timed-out watcher: purple/red dust sucks inward and swells, light pulses,
        // a rising whine — a clear "kill me or I blow" tell.
        void DetonateCharge(int ticksLeft)
        {
            if (Main.dedServ) return;
            float p = 1f - ticksLeft / (float)DetonateWindup; // 0 → 1
            int count = 1 + (int)(p * 4f);
            for (int i = 0; i < count; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float rad = MathHelper.Lerp(80f, 8f, p) + Main.rand.NextFloat(24f);
                Vector2 from = NPC.Center + ang.ToRotationVector2() * rad;
                int type = Main.rand.NextBool(3) ? DustID.RedTorch : DustID.PurpleTorch;
                int d = Dust.NewDust(from, 4, 4, type, 0f, 0f, 100, default, 1f + p * 1.6f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = (NPC.Center - from) * 0.12f;
                Main.dust[d].fadeIn = 0.3f;
            }
            Lighting.AddLight(NPC.Center, 0.4f + p, 0.1f, 0.5f + p * 0.5f);
            if (age % 24 == 0) SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.4f, Pitch = -0.6f + p }, NPC.Center);
        }

        // Timed-out detonation: gores + a big purple/red soul-cloud + a radial skull burst (the damage
        // radius) — all sized up ~40%. Fair because the 2.5s charge above telegraphs it.
        void Detonate()
        {
            if (!Main.dedServ)
            {
                UsefulFunctions.ScreenShake(NPC.Center, 7f, 22);
                SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
                for (int i = 0; i < 6; i++) // +40% gores (was ~4), bigger
                    Gore.NewGore(NPC.GetSource_Death(), NPC.Center - new Vector2(8f), Main.rand.NextVector2Circular(3.2f, 3.2f), GoreID.Smoke1 + Main.rand.Next(3), 1.35f);
                for (int i = 0; i < 46; i++) // more explosion dust
                {
                    Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame, vel.X, vel.Y, 70, default, Main.rand.NextFloat(1.4f, 2.2f));
                    Main.dust[d].noGravity = true;
                }
                for (int i = 0; i < 24; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedTorch, vel.X, vel.Y, 90, default, 1.6f);
                    Main.dust[d].noGravity = true;
                }
            }
            // Damage radius: a dense radial skull burst (~40% bigger than a normal volley).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int n = 16;
                for (int i = 0; i < n; i++)
                {
                    // Timed-out Watcher death burst: 25% slower than before (6-8.5 -> 4.5-6.375).
                    Vector2 vel = (MathHelper.TwoPi * i / n).ToRotationVector2() * Main.rand.NextFloat(4.5f, 6.375f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                        ModContent.ProjectileType<Projectiles.Enemy.VesselOfSouls.PurpleSkull>(), 15, 2f, Main.myPlayer, 0f);
                }
            }
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
        }

        public override void FindFrame(int frameHeight)
        {
            if (Main.dedServ) return;
            Player player = Main.player[NPC.target];
            int frame = 0;
            if (player.active && !player.dead)
            {
                float ang = (player.Center - NPC.Center).ToRotation();
                if (ang < 0f) ang += MathHelper.TwoPi;
                frame = ((int)Math.Round(ang / (MathHelper.TwoPi / 8f)) + GazeFrameOffset) & 7;
            }
            NPC.frame.Y = frame * frameHeight;
        }

        // Leonhard-style cached afterimage trail behind the eye.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = NPC.frame.Size() / 2f;
            for (int k = NPC.oldPos.Length - 1; k > 0; k--)
            {
                if (NPC.oldPos[k] == Vector2.Zero) continue;
                Vector2 dp = NPC.oldPos[k] + NPC.Size / 2f - screenPos;
                float f = (NPC.oldPos.Length - k) / (float)NPC.oldPos.Length;
                Color c = new Color(150, 60, 200, 0) * f * 0.35f;
                spriteBatch.Draw(tex, dp, NPC.frame, c, NPC.rotation, origin, 1f, SpriteEffects.None, 0f);
            }
            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ || NPC.life > 0) return;
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame, vel.X, vel.Y, 100, default, 1.4f);
                Main.dust[d].noGravity = true;
            }
        }
    }
}
