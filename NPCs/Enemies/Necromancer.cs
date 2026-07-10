using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    // Rebuilt with the Gigas Method (Documentation/EnemyRedesignSkillGuide.md) as a kiting ritual
    // caster: it keeps its distance and backpedals while it works, raises up to three Spellbound
    // Ghouls with a proper graveside ceremony, pressures with bone fans and grave hands between its
    // big casts, drains life through the signature Soul Siphon, EATS one of its own ghouls to heal
    // when hurt (kill the ghouls to deny it — they detonate either way), and shoves you away with a
    // curse nova when finally cornered.
    //
    // Spell color language: PURPLE shadowflame = curse (Death Strike / Grave Hands / Nova),
    // GREEN cursed-flame = soul & healing (Siphon / Sacrifice), WHITE bone = the fan.
    //
    // Poise contract at caster scale (0.4/26 in PoiseProfiles): short casts are hyper-armored; the
    // two RITUALS (Soul Siphon, Sacrifice Heal) are staggerable for their first two thirds — a poise
    // break is the interrupt now (the old justHit cast-reset is gone; it made casting stun-lockable).
    class Necromancer : ModNPC, IStaggerable
    {
        enum AttackState : byte
        {
            None = 0,
            DeathStrike,   // the classic wall-piercing curse sigil, now with a loud trail
            BoneFan,       // 5 ricocheting bone shards in a fan — its between-casts pressure
            GraveHands,    // three shadow claws erupt under the player; the third leads their escape
            SoulSiphon,    // ritual: drains the player's life into itself while range + LOS hold
            RaiseGhoul,    // ritual: a Spellbound Ghoul claws out of the ground (max 3 alive)
            SacrificeHeal, // ritual: consumes a nearby ghoul to restore 500 health
            CurseNova,     // below 50%, point-blank: shadow ring that buys the caster its distance
        }

        //Timings (ticks)
        const int StrikeTelegraphTicks = 30;
        const int FanTelegraphTicks = 25;
        const int HandsCastTicks = 75;
        const int SiphonChannelTicks = 90;
        const int SiphonStaggerableTicks = 60;  //first two thirds: a poise break cancels the drain
        const int RaiseRitualTicks = 45;
        const int SacrificeChannelTicks = 60;
        const int SacrificeStaggerableTicks = 40; //likewise interruptible
        const int NovaTelegraphTicks = 25;

        //Projectile damage (hostile projectiles deal 2x this on hit in normal mode)
        const int StrikeDamage = 18;
        const int BoneDamage = 14;
        const int HandDamage = 15;
        const int SiphonPulseDamage = 8;
        const int NovaDamage = 15;
        const int SiphonHealPerPulse = 25;
        const int SacrificeHealAmount = 500;
        const int MaxGhouls = 3;

        AttackState State = AttackState.None;
        AttackState LastAttack = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int SacrificeTarget = -1; //whoAmI of the ghoul being consumed (synced for the tether visual)

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            AnimationType = 21;
            NPC.knockBackResist = 0.2f; //overridden by the PoiseProfiles entry
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 25;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 800;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 4000; // was 270
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.NecromancerBanner>();

            // Caster levers: remember last-known position; pace when it does patrol; and NOW it kites —
            // it keeps an 8-18 tile band and backpedals while casting instead of marching into melee.
            tsorcRevampGlobalNPC casterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            casterGlobalNPC.RemembersLastKnownPos = true;
            casterGlobalNPC.PatrolMode = NPCs.PatrolMode.Pace;
            casterGlobalNPC.NavSearchRadius = 80;
            casterGlobalNPC.KiteRangeMin = 8f;
            casterGlobalNPC.KiteRangeMax = 18f;
            casterGlobalNPC.KiteLooseness = 0.2f;
        }

        //Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool oSky = (spawnInfo.SpawnTileY < (Main.maxTilesY * 0.1f));
            bool oSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.3f));
            bool oUnderground = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.4f));
            bool oCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.6f));
            bool oMagmaCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.8f));
            bool oUnderworld = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.8f));

            if (spawnInfo.Player.townNPCs > 0f || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneMeteor) return 0;

            if (spawnInfo.Water) return 0f;

            if (!Main.hardMode)
            {
                if (spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(3000)) return 1;
                if (oUnderworld && Main.rand.NextBool(500)) return 1;
                if (oUnderworld && !Main.dayTime && Main.rand.NextBool(200)) return 1;
                if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.35f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && (oUnderground || oCavern) && Main.rand.NextBool(2000)) return 1;
                return 0;
            }
            else if (Main.hardMode)
            {
                if (oUnderworld && Main.dayTime && Main.rand.NextBool(60)) return 1;
                if (oUnderworld && !Main.dayTime && Main.rand.NextBool(35)) return 1;
                if (spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;
                if (spawnInfo.Player.ZoneHallow && (oUnderground || oCavern) && Main.rand.NextBool(90)) return 1;
                if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUndergroundDesert) && (oUnderground || oCavern) && Main.rand.NextBool(100)) return 1;
                return 0;
            }
            return 0;
        }
        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((short)SacrificeTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            SacrificeTarget = reader.ReadInt16();
        }

        ///<summary>Its casting hand — where the spell color language originates.</summary>
        Vector2 HandTip => NPC.Center + new Vector2(NPC.direction * 16f, -8f);

        ///<summary>Poise break (neutral or a ritual's staggerable window): the working fizzles.</summary>
        public void OnStagger(NPC npc)
        {
            if (State != AttackState.None && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, vel.X, vel.Y, 90, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            SacrificeTarget = -1;
            AttackCooldown = Math.Max(AttackCooldown, 70);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[NPC.target];

            g.AttackTelegraphing = false;
            g.AttackCommitted = false;

            //Staggered: rooted by the global system
            if (g.StaggerTimer > 0)
            {
                NPC.rotation = MathHelper.Lerp(NPC.rotation, -NPC.direction * 0.2f, 0.1f);
                return;
            }
            NPC.rotation *= 0.85f;

            //Idle identity: stray shadow wisps — a thing that shouldn't be walking
            if (Main.rand.NextBool(4))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                int wisp = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, -0.5f, 150, default, 0.8f);
                Main.dust[wisp].noGravity = true;
                Main.dust[wisp].velocity *= 0.25f;
            }
            Lighting.AddLight(NPC.Center, 0.15f, 0.08f, 0.22f);

            if (State == AttackState.None)
            {
                tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.07f, canTeleport: true, lavaJumping: true, canPounce: false, canWalkBackwards: true);

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
            bool los = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            bool belowHalf = NPC.life < NPC.lifeMax / 2;
            bool hurt = NPC.life < NPC.lifeMax * 0.7f;
            int ghouls = CountGhouls();
            bool ghoulNearby = FindNearestGhoul(640f) != null;

            Span<(AttackState state, float weight)> pool = stackalloc (AttackState, float)[]
            {
                (AttackState.DeathStrike,   1f),
                (AttackState.BoneFan,       los ? 1f : 0.2f),
                (AttackState.GraveHands,    0.8f),
                (AttackState.SoulSiphon,    distTiles < 14f && los ? 0.9f : 0f),
                (AttackState.RaiseGhoul,    ghouls < MaxGhouls ? 1.1f : 0f),
                (AttackState.SacrificeHeal, hurt && ghoulNearby ? 1.2f : 0f),
                (AttackState.CurseNova,     belowHalf && distTiles < 7f ? 1.3f : 0f),
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
            AttackState chosen = AttackState.DeathStrike;
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f)
                {
                    chosen = pool[i].state;
                    break;
                }
            }
            //Lock the sacrifice victim now so clients can draw the tether
            if (chosen == AttackState.SacrificeHeal)
            {
                NPC ghoul = FindNearestGhoul(640f);
                SacrificeTarget = ghoul != null ? ghoul.whoAmI : -1;
                if (SacrificeTarget < 0)
                {
                    chosen = AttackState.DeathStrike;
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
            SacrificeTarget = -1;
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
            NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
            NPC.velocity.X *= 0.85f;

            AttackTimer++;
            switch (State)
            {
                case AttackState.DeathStrike: RunDeathStrike(g, player); break;
                case AttackState.BoneFan: RunBoneFan(g, player); break;
                case AttackState.GraveHands: RunGraveHands(g, player); break;
                case AttackState.SoulSiphon: RunSoulSiphon(g, player); break;
                case AttackState.RaiseGhoul: RunRaiseGhoul(g); break;
                case AttackState.SacrificeHeal: RunSacrificeHeal(g); break;
                case AttackState.CurseNova: RunCurseNova(g); break;
            }
        }

        void RunDeathStrike(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= StrikeTelegraphTicks;
            if (AttackTimer <= StrikeTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }
                //PURPLE: curse gathering into the casting hand
                float progress = AttackTimer / (float)StrikeTelegraphTicks;
                for (int i = 0; i < 1 + (int)(progress * 2f); i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = HandTip + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 26f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (HandTip - pos) * 0.15f;
                }
                Lighting.AddLight(HandTip, 0.35f * progress, 0.15f * progress, 0.5f * progress);

                if (AttackTimer == StrikeTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = (player.Center - HandTip).SafeNormalize(new Vector2(NPC.direction, 0f)) * 8f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), HandTip, vel,
                        ModContent.ProjectileType<EnemySpellSuddenDeathStrike>(), StrikeDamage, 1f, Main.myPlayer);
                }
            }
            else if (AttackTimer >= StrikeTelegraphTicks + 25) //recovery, no armor
            {
                EndAttack(180);
            }
        }

        void RunBoneFan(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= FanTelegraphTicks;
            if (AttackTimer <= FanTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center); //dry rattle
                }
                //WHITE: bone dust rattling around the hand
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(HandTip - new Vector2(8f, 8f), 16, 16, DustID.Bone, 0f, -0.5f, 80, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.3f;
                }
                if (AttackTimer == FanTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aim = (player.Center - HandTip).SafeNormalize(new Vector2(NPC.direction, 0f));
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 vel = aim.RotatedBy(i * 0.175f) * 8.5f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), HandTip, vel,
                            ModContent.ProjectileType<NecroBoneShard>(), BoneDamage, 2f, Main.myPlayer, 0.12f);
                    }
                    SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.7f, Pitch = 0.2f }, NPC.Center);
                }
            }
            else if (AttackTimer >= FanTelegraphTicks + 20)
            {
                EndAttack(150);
            }
        }

        void RunGraveHands(tsorcRevampGlobalNPC g, Player player)
        {
            g.AttackCommitted = AttackTimer <= HandsCastTicks - 20; //the tail is already recovery
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
            }
            //PURPLE at the hand while the graves open under the player
            if (AttackTimer <= HandsCastTicks - 20 && Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(HandTip - new Vector2(6f, 6f), 12, 12, DustID.Shadowflame, 0f, -1f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Two hands chase the player, the third leads their escape
                if (AttackTimer == 15 || AttackTimer == 35)
                {
                    SpawnGraveHand(player.Center.X);
                }
                if (AttackTimer == 55)
                {
                    SpawnGraveHand(player.Center.X + player.velocity.X * 25f);
                }
            }
            if (AttackTimer >= HandsCastTicks + 20)
            {
                EndAttack(240);
            }
        }

        void RunSoulSiphon(tsorcRevampGlobalNPC g, Player player)
        {
            //RITUAL: staggerable for the first two thirds — the poise break is the interrupt
            if (AttackTimer < SiphonStaggerableTicks)
            {
                g.AttackTelegraphing = true;
            }
            else if (AttackTimer <= SiphonChannelTicks)
            {
                g.AttackCommitted = true;
            }
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
            }
            if (AttackTimer == SiphonStaggerableTicks)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //commit cue
            }

            if (AttackTimer <= SiphonChannelTicks)
            {
                //Range or line of sight broken: the thread snaps
                bool connected = NPC.Distance(player.Center) < 224f && Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
                if (!connected)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int dust = Dust.NewDust(HandTip - new Vector2(8f, 8f), 16, 16, DustID.CursedTorch, 0f, -1f, 120, default, 1f);
                        Main.dust[dust].noGravity = true;
                    }
                    EndAttack(120);
                    return;
                }
                //GREEN: the tether — soul wisps flowing FROM the player TO the necromancer
                for (int i = 0; i < 2; i++)
                {
                    float t = Main.rand.NextFloat();
                    Vector2 pos = Vector2.Lerp(player.Center, HandTip, t) + Main.rand.NextVector2Circular(8f, 8f);
                    int wisp = Dust.NewDust(pos, 4, 4, DustID.CursedTorch, 0f, 0f, 80, default, 1.1f);
                    Main.dust[wisp].noGravity = true;
                    Main.dust[wisp].velocity = (HandTip - player.Center).SafeNormalize(Vector2.UnitX) * 4f;
                }
                //Drain pulses after a warm-up: each bites the player and feeds the caster
                if (AttackTimer >= 30 && (AttackTimer - 30) % 20 == 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, Vector2.Zero,
                            ModContent.ProjectileType<NecroSiphonPulse>(), SiphonPulseDamage, 0f, Main.myPlayer);
                    }
                    NPC.life = Math.Min(NPC.lifeMax, NPC.life + SiphonHealPerPulse);
                    NPC.HealEffect(SiphonHealPerPulse);
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f, Pitch = 0.5f }, NPC.Center);
                }
            }
            else if (AttackTimer >= SiphonChannelTicks + 25) //recovery, no armor
            {
                EndAttack(300);
            }
        }

        void RunRaiseGhoul(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = AttackTimer <= RaiseRitualTicks;
            Vector2 grave = NPC.Center + new Vector2(NPC.direction * 4 * 16f, 0f);
            float groundY = FindGroundY(grave - new Vector2(0f, 32f), 10);

            if (AttackTimer <= RaiseRitualTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                }
                if (groundY > 0f)
                {
                    //The grave circle: dark simmer + rising glints where the ghoul will claw out
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = new Vector2(grave.X - 20f + Main.rand.NextFloat(40f), groundY - 6f);
                        int dust = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, -1.5f, 110, default, 1.1f);
                        Main.dust[dust].noGravity = true;
                    }
                }
                if (AttackTimer == RaiseRitualTicks && Main.netMode != NetmodeID.MultiplayerClient
                    && groundY > 0f && CountGhouls() < MaxGhouls)
                {
                    int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)grave.X, (int)groundY, ModContent.NPCType<SpellboundGhoul>());
                    if (spawned < Main.maxNPCs)
                    {
                        Main.npc[spawned].velocity.Y = -5f; //claws up out of the earth
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
                        }
                    }
                    SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.6f, Pitch = -0.4f }, grave);
                    for (int i = 0; i < 16; i++)
                    {
                        int dirt = Dust.NewDust(new Vector2(grave.X - 20f, groundY - 12f), 40, 12, DustID.Dirt, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-5f, -2f), 60, default, 1.3f);
                        Main.dust[dirt].velocity.Y -= 1f;
                        int wisp = Dust.NewDust(new Vector2(grave.X - 20f, groundY - 20f), 40, 20, DustID.Shadowflame, 0f, -2f, 80, default, 1.2f);
                        Main.dust[wisp].noGravity = true;
                    }
                }
            }
            else if (AttackTimer >= RaiseRitualTicks + 15)
            {
                EndAttack(360);
            }
        }

        void RunSacrificeHeal(tsorcRevampGlobalNPC g)
        {
            NPC ghoul = SacrificeTarget >= 0 && SacrificeTarget < Main.maxNPCs ? Main.npc[SacrificeTarget] : null;
            bool valid = ghoul != null && ghoul.active && ghoul.type == ModContent.NPCType<SpellboundGhoul>();
            if (!valid)
            {
                //The victim died first: heal denied
                EndAttack(150);
                return;
            }

            //RITUAL: staggerable for the first two thirds
            if (AttackTimer < SacrificeStaggerableTicks)
            {
                g.AttackTelegraphing = true;
            }
            else if (AttackTimer <= SacrificeChannelTicks)
            {
                g.AttackCommitted = true;
            }
            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.8f }, NPC.Center);
            }
            if (AttackTimer == SacrificeStaggerableTicks)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0f }, NPC.Center); //commit cue
            }

            if (AttackTimer <= SacrificeChannelTicks)
            {
                //GREEN: the ghoul's essence streaming to the necromancer; the victim shudders
                for (int i = 0; i < 2; i++)
                {
                    float t = Main.rand.NextFloat();
                    Vector2 pos = Vector2.Lerp(ghoul.Center, HandTip, t) + Main.rand.NextVector2Circular(6f, 6f);
                    int wisp = Dust.NewDust(pos, 4, 4, DustID.CursedTorch, 0f, 0f, 70, default, 1.1f);
                    Main.dust[wisp].noGravity = true;
                    Main.dust[wisp].velocity = (HandTip - ghoul.Center).SafeNormalize(Vector2.UnitX) * 4f;
                }
                if (Main.rand.NextBool(3))
                {
                    int shudder = Dust.NewDust(ghoul.position, ghoul.width, ghoul.height, DustID.Shadowflame, 0f, -1f, 100, default, 1f);
                    Main.dust[shudder].noGravity = true;
                }

                if (AttackTimer == SacrificeChannelTicks)
                {
                    //Consume it
                    SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.7f, Pitch = 0.2f }, ghoul.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ghoul.life = 0;
                        ghoul.HitEffect();
                        ghoul.checkDead();
                    }
                    NPC.life = Math.Min(NPC.lifeMax, NPC.life + SacrificeHealAmount);
                    NPC.HealEffect(SacrificeHealAmount);
                    for (int i = 0; i < 20; i++)
                    {
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, 0f, -2f, 60, default, 1.4f);
                        Main.dust[dust].noGravity = true;
                    }
                }
            }
            else if (AttackTimer >= SacrificeChannelTicks + 15)
            {
                EndAttack(420);
            }
        }

        void RunCurseNova(tsorcRevampGlobalNPC g)
        {
            g.AttackCommitted = AttackTimer <= NovaTelegraphTicks;
            if (AttackTimer <= NovaTelegraphTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
                }
                //PURPLE spiralling inward — get off it NOW
                float progress = AttackTimer / (float)NovaTelegraphTicks;
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(70f, 12f, progress) + Main.rand.NextFloat(10f);
                    Vector2 pos = NPC.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, 0f, 90, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (NPC.Center - pos) * 0.1f;
                }
                if (AttackTimer == NovaTelegraphTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<NecroCurseNova>(), NovaDamage, 9f, Main.myPlayer, 112f);
                    }
                }
            }
            else if (AttackTimer >= NovaTelegraphTicks + 20)
            {
                EndAttack(330);
            }
        }

        #region Helpers

        void SpawnGraveHand(float x)
        {
            float groundY = FindGroundY(new Vector2(x, Main.player[NPC.target].Bottom.Y - 8f), 20);
            if (groundY < 0f)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, groundY - 23f), Vector2.Zero,
                ModContent.ProjectileType<NecroGraveHand>(), HandDamage, 1f, Main.myPlayer, 28f);
        }

        int CountGhouls()
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<SpellboundGhoul>())
                {
                    count++;
                }
            }
            return count;
        }

        NPC FindNearestGhoul(float maxDist)
        {
            NPC best = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.type != ModContent.NPCType<SpellboundGhoul>())
                {
                    continue;
                }
                float dist = Vector2.Distance(npc.Center, NPC.Center);
                if (dist < maxDist)
                {
                    maxDist = dist;
                    best = npc;
                }
            }
            return best;
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

        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                //Its collected souls escape: a spiral of wisps before the body falls
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
                for (int i = 0; i < 24; i++)
                {
                    float angle = MathHelper.TwoPi * i / 24f;
                    int wisp = Dust.NewDust(NPC.Center + angle.ToRotationVector2() * 8f, 4, 4, DustID.Shadowflame, 0f, 0f, 60, default, 1.5f);
                    Main.dust[wisp].noGravity = true;
                    Main.dust[wisp].velocity = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 2.5f + new Vector2(0f, -3f);
                }
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Necromancer Gore 3").Type, 1.1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Mobility.BootsOfHaste>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 25));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 11));
        }
    }
}
