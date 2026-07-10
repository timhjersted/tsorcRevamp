using Terraria.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies{
	// Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
	//
	// Fire-staff caster, rebuilt with the Gigas Method (see Documentation/EnemyRedesignSkillGuide.md)
	// at common-enemy scale: 5 attacks (2 gated below half health), short telegraphs, brisk cooldowns.
	// All attack language is dust and projectiles — the sprite only has walk/idle/jump frames, but it
	// holds a staff, so casts read from a glowing staff tip. Its historic identity (the old
	// MNPC.teleporterAI) survives as the Blink Volley. Below half health the beast under the robes
	// comes out: a hyper-armored horn charge and a desperation cinder burst.
	//
	// Poise contract at small scale: standard-fighter tier (35f in PoiseProfiles). Short telegraphs
	// are hyper-armored; the one long cast (Ring of Cinders) is staggerable for its first two thirds.
	// Recovery windows have no armor — same rules as the giants, much smaller numbers.
	public class MinotaurMage : ModNPC, IStaggerable
	{
        enum AttackState : byte
        {
            None = 0,
            EmberBolts,     // staff glint → three quick fire bolts
            FlameWave,      // staff slam → short ground-crawling fire waves both ways
            BlinkVolley,    // the old teleporter identity: blink to the player's flank, two snap bolts
            BullhornCharge, // below 50%: snorting windup → hyper-armored horn rush, big punish recovery
            RingOfCinders,  // below 50%: long staggerable cast → radial arcing embers
        }

        //Timings (ticks)
        const int BoltsTelegraphTicks = 25;
        const int WaveTelegraphTicks = 30;
        const int BlinkTelegraphTicks = 18;
        const int ChargeTelegraphTicks = 35;
        const int ChargeTicks = 55;
        const int ChargeRecoveryTicks = 45;
        const int CindersTelegraphTicks = 40;
        const int CindersStaggerableTicks = 26; //first two thirds: a poise break cancels the cast

        //Projectile damage (hostile projectiles deal 2x this on hit in normal mode)
        const int BoltDamage = 12;
        const int WaveDamage = 14;
        const int CinderDamage = 11;
        const float ChargeSpeed = 5.5f;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int lockedDir = 1;          //horn-charge direction, locked at charge start
        bool AngrySnortDone;        //one-time below-half flourish
        int baseContactDamage = -1; //contact damage to restore after the charge's boost

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPC.type] = 6; //afterimages for the horn charge
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }

        public override void SetDefaults()
		{
			NPC.width = 20;
			NPC.height = 40;
			NPC.damage = 9;
			NPC.defense = 4;
			NPC.lifeMax = 155;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 5000f;
			NPC.npcSlots = 100;
            NPC.scale = 1.1f;
			NPC.knockBackResist = 0.6f; //overridden by the PoiseProfiles entry
			Main.npcFrameCount[NPC.type] = 15;
			AnimationType = 21;
		}

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((sbyte)lockedDir);
            writer.Write(AngrySnortDone);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            lockedDir = reader.ReadSByte();
            AngrySnortDone = reader.ReadBoolean();
        }

        ///<summary>The glowing tip of its staff — where every cast's dust language originates.</summary>
        Vector2 StaffTip => NPC.Center + new Vector2(NPC.direction * 18f, -14f);

        ///<summary>Poise break (neutral or the Ring of Cinders staggerable window): the cast fizzles.</summary>
        public void OnStagger(NPC npc)
        {
            if (State != AttackState.None && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, vel.X, vel.Y, 100, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            AttackCooldown = Math.Max(AttackCooldown, 60);
        }

		public override void AI()
		{
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];
            if (baseContactDamage < 0)
            {
                baseContactDamage = NPC.damage;
            }

            g.AttackTelegraphing = false;
            g.AttackCommitted = false;

            //Staggered: rooted by the global system; slumped lean
            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.2f, 0.1f);
                return;
            }
            NPC.rotation *= 0.85f;

            //One-time below-half flourish: an angry snort, and the phase attacks unlock
            if (!AngrySnortDone && NPC.life < NPC.lifeMax / 2)
            {
                AngrySnortDone = true;
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.35f, Pitch = 0.5f }, NPC.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 nose = NPC.Center + new Vector2(NPC.direction * 12f, -10f);
                    int puff = Dust.NewDust(nose, 8, 8, DustID.Smoke, NPC.direction * 2f, 0f, 120, default, 1.1f);
                    Main.dust[puff].velocity *= 0.5f;
                }
                NPC.netUpdate = true;
            }

            if (State == AttackState.None)
            {
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 1.3f, acceleration: 0.7f, canTeleport: false);

                //Idle staff glow — its element reads at a glance
                if (Main.rand.NextBool(4))
                {
                    int glow = Dust.NewDust(StaffTip, 4, 4, DustID.Torch, 0f, -0.5f, 120, default, 0.8f);
                    Main.dust[glow].noGravity = true;
                    Main.dust[glow].velocity *= 0.2f;
                }
                Lighting.AddLight(StaffTip, 0.25f, 0.15f, 0.05f);

                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackCooldown <= 0 && NPC.velocity.Y == 0f
                    && !player.dead && player.active && NPC.Distance(player.Center) < 1000f)
                {
                    PickAttack(player);
                }
            }
            else
            {
                RunAttack(g, player);
            }
		}

        void PickAttack(Player player)
        {
            float distTiles = NPC.Distance(player.Center) / 16f;
            bool sameLevel = Math.Abs(player.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;
            bool belowHalf = NPC.life < NPC.lifeMax / 2;

            Span<(AttackState state, float weight)> pool = stackalloc (AttackState, float)[]
            {
                (AttackState.EmberBolts,     1f),
                (AttackState.FlameWave,      distTiles < 12f ? 1.1f : 0.2f),
                (AttackState.BlinkVolley,    0.7f),
                (AttackState.BullhornCharge, belowHalf && distTiles > 6f && distTiles < 30f && sameLevel ? 1.2f : 0f),
                (AttackState.RingOfCinders,  !belowHalf ? 0f : distTiles < 10f ? 1f : 0.4f),
            };
            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].state == LastAttack)
                {
                    pool[i].weight *= 0.5f;
                }
                total += pool[i].weight;
            }
            float roll = Main.rand.NextFloat(total);
            AttackState chosen = AttackState.EmberBolts;
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    chosen = pool[i].state;
                    break;
                }
            }
            State = chosen;
            AttackTimer = 0;
            NPC.netUpdate = true;
        }

        void EndAttack(int cooldown)
        {
            LastAttack = State;
            State = AttackState.None;
            AttackTimer = 0;
            //Safety: restore anything an attack changed
            NPC.damage = baseContactDamage;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AttackCooldown = cooldown + Main.rand.Next(40);
                NPC.netUpdate = true;
            }
        }

        void RunAttack(tsorcRevampGlobalNPC g, Player player)
        {
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }
            //Face the player, except mid-charge (direction locked at its start)
            if (!(State == AttackState.BullhornCharge && AttackTimer > ChargeTelegraphTicks))
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = NPC.direction;

            AttackTimer++;
            switch (State)
            {
                case AttackState.EmberBolts: RunEmberBolts(g, player); break;
                case AttackState.FlameWave: RunFlameWave(g); break;
                case AttackState.BlinkVolley: RunBlinkVolley(g, player); break;
                case AttackState.BullhornCharge: RunBullhornCharge(g); break;
                case AttackState.RingOfCinders: RunRingOfCinders(g); break;
            }
        }

        void RunEmberBolts(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= BoltsTelegraphTicks + 16;
            NPC.velocity.X *= 0.85f;

            if (AttackTimer <= BoltsTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.2f }, NPC.Center);
                }
                //Staff tip ramps up: fire gathering into the cast point
                float progress = AttackTimer / (float)BoltsTelegraphTicks;
                for (int i = 0; i < 1 + (int)(progress * 2f); i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = StaffTip + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 24f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (StaffTip - pos) * 0.15f;
                }
                Lighting.AddLight(StaffTip, 0.5f * progress, 0.3f * progress, 0.1f * progress);
            }
            //Three quick bolts, re-aimed each shot with a little spread
            if ((AttackTimer == BoltsTelegraphTicks || AttackTimer == BoltsTelegraphTicks + 8 || AttackTimer == BoltsTelegraphTicks + 16)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = (player.Center - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f)).RotatedBy(Main.rand.NextFloat(-0.08f, 0.08f)) * 9f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, vel,
                    ModContent.ProjectileType<MinotaurEmberBolt>(), BoltDamage, 1f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
            }
            if (AttackTimer >= BoltsTelegraphTicks + 16 + 24) //short recovery, no armor
            {
                EndAttack(140);
            }
        }

        void RunFlameWave(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = AttackTimer <= WaveTelegraphTicks;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= WaveTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.5f }, NPC.Center);
                }
                //Fire builds at the staff tip while the ground at its feet simmers — the wave's read
                int dust = Dust.NewDust(StaffTip, 4, 4, DustID.Torch, 0f, -1f, 90, default, 1.2f);
                Main.dust[dust].noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - 6f);
                    int simmer = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, -1.5f, 110, default, 1f);
                    Main.dust[simmer].noGravity = true;
                }
                if (AttackTimer == WaveTelegraphTicks)
                {
                    //Staff slam: both waves, armed a beat after the impact dust
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Bottom);
                    UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 10);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int dir = -1; dir <= 1; dir += 2)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom + new Vector2(dir * 16f, -16f), Vector2.Zero,
                                ModContent.ProjectileType<MinotaurFlameWave>(), WaveDamage, 2f, Main.myPlayer, dir, 8f);
                        }
                    }
                }
            }
            else
            {
                NPC.velocity.X *= 0.7f; //recovery, no armor
                if (AttackTimer >= WaveTelegraphTicks + 30)
                {
                    EndAttack(160);
                }
            }
        }

        void RunBlinkVolley(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= BlinkTelegraphTicks + 14;
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= BlinkTelegraphTicks)
            {
                //Winding up the blink: the classic white teleport shimmer, in fire
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 120, Color.White, 1.2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (NPC.Center - Main.dust[dust].position) * 0.1f;
                }
                if (AttackTimer == BlinkTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Blink to the player's flank, ground-snapped; stay put if there's nowhere to go
                    int side = Main.rand.NextBool() ? 1 : -1;
                    float destX = player.Center.X + side * Main.rand.NextFloat(10f, 14f) * 16f;
                    float groundY = FindGroundY(new Vector2(destX, player.Bottom.Y - 48f), 15);
                    if (groundY > 0f)
                    {
                        BlinkFlash();
                        NPC.Bottom = new Vector2(destX, groundY);
                        NPC.velocity = Vector2.Zero;
                        BlinkFlash();
                        NPC.netUpdate = true;
                    }
                }
            }
            //Two snap bolts right out of the blink — the punish for standing still
            if ((AttackTimer == BlinkTelegraphTicks + 6 || AttackTimer == BlinkTelegraphTicks + 14)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = (player.Center - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f)) * 9.5f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, vel,
                    ModContent.ProjectileType<MinotaurEmberBolt>(), BoltDamage, 1f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.4f }, NPC.Center);
            }
            if (AttackTimer >= BlinkTelegraphTicks + 14 + 22) //recovery, no armor
            {
                EndAttack(150);
            }
        }

        void RunBullhornCharge(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= ChargeTelegraphTicks)
            {
                g.AttackCommitted = true;
                NPC.velocity.X *= 0.8f;
                lockedDir = NPC.direction;
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.4f, Pitch = 0.5f }, NPC.Center);
                }
                //Pawing the ground + snorting smoke — unmistakably a bull about to run you down
                if (AttackTimer % 12 < 3)
                {
                    Vector2 hoof = NPC.Bottom + new Vector2(NPC.direction * 8f, -4f);
                    int dust = Dust.NewDust(hoof, 8, 4, DustID.Smoke, -NPC.direction * 2f, -1f, 100, default, 1f);
                    Main.dust[dust].velocity *= 0.5f;
                }
                if (AttackTimer % 15 == 0)
                {
                    Vector2 nose = NPC.Center + new Vector2(NPC.direction * 12f, -10f);
                    int puff = Dust.NewDust(nose, 8, 8, DustID.Smoke, NPC.direction * 2.5f, 0f, 120, default, 1.1f);
                    Main.dust[puff].velocity *= 0.6f;
                }
            }
            else if (AttackTimer <= ChargeTelegraphTicks + ChargeTicks)
            {
                //The rush: hyper-armored, harder contact hit, afterimage trail (PreDraw)
                g.AttackCommitted = true;
                NPC.damage = baseContactDamage * 2;
                NPC.velocity.X = lockedDir * ChargeSpeed;
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(NPC.position + new Vector2(0f, NPC.height - 8f), NPC.width, 8, DustID.Smoke, -lockedDir * 2f, -0.5f, 120, default, 1f);
                    Main.dust[dust].velocity *= 0.4f;
                }
                //Hit a wall: skip straight to the recovery
                if (NPC.collideX && AttackTimer > ChargeTelegraphTicks + 8)
                {
                    AttackTimer = ChargeTelegraphTicks + ChargeTicks;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                //Blown past you and winded: the punish window
                NPC.damage = baseContactDamage;
                NPC.velocity.X *= 0.8f;
                if (Main.rand.NextBool(3))
                {
                    Vector2 nose = NPC.Center + new Vector2(NPC.direction * 12f, -10f);
                    int puff = Dust.NewDust(nose, 8, 8, DustID.Smoke, NPC.direction * 1.5f, 0.3f, 140, default, 0.9f);
                    Main.dust[puff].velocity *= 0.4f;
                }
                if (AttackTimer >= ChargeTelegraphTicks + ChargeTicks + ChargeRecoveryTicks)
                {
                    EndAttack(220);
                }
            }
        }

        void RunRingOfCinders(tsorcRevampGlobalNPC g)
        {
            if (AttackTimer <= CindersTelegraphTicks)
            {
                NPC.velocity.X *= 0.8f;
                //The one long cast: staggerable for its first two thirds, then committed
                if (AttackTimer < CindersStaggerableTicks)
                {
                    g.AttackTelegraphing = true;
                }
                else
                {
                    g.AttackCommitted = true;
                }
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
                }
                if (AttackTimer == CindersStaggerableTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //commit cue
                }
                //Fire spiralling inward to the staff
                float progress = AttackTimer / (float)CindersTelegraphTicks;
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(60f, 10f, progress) + Main.rand.NextFloat(10f);
                    Vector2 pos = StaffTip + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (StaffTip - pos) * 0.08f;
                }
                Lighting.AddLight(StaffTip, 0.6f * progress, 0.35f * progress, 0.1f * progress);

                if (AttackTimer == CindersTelegraphTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, vel.X, vel.Y, 60, default, 1.5f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Radial arcing embers — the gaps between arcs are the dodge
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 vel = (MathHelper.TwoPi * i / 8f + Main.rand.NextFloat(0.2f)).ToRotationVector2() * 7f;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                ModContent.ProjectileType<MinotaurCinder>(), CinderDamage, 1f, Main.myPlayer, 0.2f);
                        }
                    }
                }
            }
            else
            {
                NPC.velocity.X *= 0.7f; //recovery, no armor
                if (AttackTimer >= CindersTelegraphTicks + 35)
                {
                    EndAttack(200);
                }
            }
        }

        void BlinkFlash()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            for (int m = 0; m < 20; m++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0, 0, 100, Color.White, 1.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Main.rand.NextVector2Circular(6f, 6f);
            }
        }

        ///<summary>World Y of the first solid tile top under the point (scanning down), or -1 if none in range.</summary>
        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }

        ///<summary>Smoke-and-ember afterimage trail during the horn charge.</summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool charging = State == AttackState.BullhornCharge && AttackTimer > ChargeTelegraphTicks && AttackTimer <= ChargeTelegraphTicks + ChargeTicks;
            if (charging || NPC.velocity.Length() > 7f)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height); //bottom-center
                for (int k = NPC.oldPos.Length - 1; k > 0; k -= 2)
                {
                    Vector2 drawPos = NPC.oldPos[k] + new Vector2(NPC.width / 2f, NPC.height + NPC.gfxOffY + 4f) - screenPos;
                    Color color = new Color(255, 140, 60, 0) * ((NPC.oldPos.Length - k) / (float)NPC.oldPos.Length) * 0.5f;
                    spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, origin, NPC.scale, effects, 0f);
                }
            }
            return true;
        }

        public override void OnKill()
        {
            //Its fire escapes it: the classic torch-dust burst, kept from the original
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0, 0, 100, default, 1.5f);
                Main.dust[dust].noGravity = false;
            }
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("MinotaurGore3").Type, 1f);
        }
    }
}
