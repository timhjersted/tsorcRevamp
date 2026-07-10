using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    ///<summary>
    ///Parasprite: the multiplying Hallow gnat, reworked so its identity is visible and fightable.
    ///Color = role: PINK ones are the breeders (they visibly BLOAT for a second before splitting —
    ///kill them mid-bloat to deny the copy); blue and gold never multiply. Any of them may wind up
    ///and DART at the player: on contact it latches and inflicts Bleeding (Poison too in SHM).
    ///Latched on your front (relative to your current facing), a swing kills it — latched on your
    ///back it can't be hit, and only a dodge roll shakes it off. Turning around swaps which is which.
    ///When one dies, nearby parasprites panic and scatter. Spawns only in the hallowed surface and
    ///hallowed underground now (no more sky swarms), and persists into SHM with escalated stats.
    ///</summary>
    class Parasprite : ModNPC
    {
        //Sheet rows: 1-2 blue, 3-4 gold, 5-6 pink. Pink is the breeder.
        const int BreederColor = 5;
        const int MaxSwarm = 20;
        const int MaxAttached = 3;
        const int BloatTicks = 60;
        const int DartWindupTicks = 20;
        const int DartMaxTicks = 40;
        const int MaxLatchTicks = 600;
        const float DartSpeed = 9f;

        enum Mode : byte { Drift, DartWindup, Darting, Attached, Bloat }

        Mode mode = Mode.Drift;
        int modeTimer;
        int breedTimer;
        int dartCooldown = 180;
        int attachSide = 1;       //world side (-1/1) it latched onto; front/back is vs the player's CURRENT facing
        int spriteColor;          //1 blue, 3 gold, 5 pink — server-assigned, synced (it's the breeder flag now)
        bool statsInitialized;
        bool SHM;
        int baseDamage;
        public int panicTimer;    //set by dying neighbors: scatter burst

        public bool IsAttached => mode == Mode.Attached;
        bool Breeder => spriteColor == BreederColor;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.width = 12;
            NPC.height = 12;
            NPC.aiStyle = 22;
            NPC.damage = 48;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 90; //survives one modest hit and gets knocked flying
            NPC.friendly = false;
            NPC.noTileCollide = false;
            NPC.noGravity = true;
            NPC.defense = 1;
            NPC.knockBackResist = 1f; //a fly SHOULD scatter when smacked
            NPC.value = 80;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ParaspriteBanner>();
        }

        ///<summary>Now exclusively a Hallow pest: hallowed surface + hallowed underground, no sky,
        ///and it keeps spawning into SHM (with escalated stats) instead of vanishing.</summary>
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Water || !spawnInfo.Player.ZoneHallow || !NPC.downedBoss3)
            {
                return 0f;
            }
            //Surface down through the cavern — no sky, no underworld
            if (spawnInfo.SpawnTileY < Main.maxTilesY * 0.1f || spawnInfo.SpawnTileY > Main.maxTilesY * 0.6f)
            {
                return 0f;
            }
            return 0.15f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)mode);
            writer.Write((short)modeTimer);
            writer.Write((byte)spriteColor);
            writer.Write((sbyte)attachSide);
            writer.Write(SHM);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            mode = (Mode)reader.ReadByte();
            modeTimer = reader.ReadInt16();
            spriteColor = reader.ReadByte();
            attachSide = reader.ReadSByte();
            SHM = reader.ReadBoolean();
        }

        public override bool PreAI()
        {
            //Vanilla hovering AI only drives the neutral drift; every other mode owns the body
            NPC.aiStyle = mode == Mode.Drift ? 22 : -1;
            return true;
        }

        public override void AI()
        {
            if (!statsInitialized && Main.netMode != NetmodeID.MultiplayerClient)
            {
                statsInitialized = true;
                spriteColor = (Main.rand.Next(3) * 2) + 1; //1, 3 or 5 — role assigned at birth
                if (tsorcRevampWorld.SuperHardMode)
                {
                    SHM = true;
                    NPC.lifeMax = 270;
                    NPC.life = NPC.lifeMax;
                    NPC.damage = 90;
                }
                baseDamage = NPC.damage;
                NPC.netUpdate = true;
            }
            if (baseDamage == 0 && mode != Mode.Attached)
            {
                baseDamage = NPC.damage; //client-side fallback
            }
            Player player = Main.player[NPC.target];

            //Contact damage only when it's flying free — the latch bleeds instead of bumping
            NPC.damage = mode == Mode.Attached ? 0 : baseDamage;
            if (mode != Mode.Attached)
            {
                NPC.dontTakeDamage = false;
            }

            switch (mode)
            {
                case Mode.Drift: RunDrift(player); break;
                case Mode.DartWindup: RunDartWindup(player); break;
                case Mode.Darting: RunDarting(player); break;
                case Mode.Attached: RunAttached(player); break;
                case Mode.Bloat: RunBloat(); break;
            }
        }

        void RunDrift(Player player)
        {
            //The classic erratic flutter on top of the vanilla hover
            NPC.velocity.Y += Main.rand.Next(-10, 10) / 8;
            NPC.velocity.X += Main.rand.Next(-10, 10) / 20;
            NPC.scale = MathHelper.Lerp(NPC.scale, 1f, 0.2f);

            //Panic: a neighbor just died — scatter, fast
            if (panicTimer > 0)
            {
                panicTimer--;
                NPC.velocity *= 1.04f;
                if (NPC.velocity.Length() > 7f)
                {
                    NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitX) * 7f;
                }
            }

            //Faint hallow flutter
            if (Main.rand.NextBool(8))
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.HallowSpray, 0f, 0f, 120, default, 0.7f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            //Dart at the player: capped so a swarm can't stack-drain
            if (dartCooldown > 0)
            {
                dartCooldown--;
            }
            if (dartCooldown <= 0 && !player.dead && player.active && NPC.Distance(player.Center) < 320f
                && CountAttached() < MaxAttached && Main.rand.NextBool(60))
            {
                mode = Mode.DartWindup;
                modeTimer = 0;
                NPC.netUpdate = true;
                return;
            }

            //Breeding: PINK ones only, and the bloat telegraph is the counterplay window
            if (Breeder)
            {
                breedTimer++;
                if (breedTimer >= 300)
                {
                    breedTimer = -Main.rand.Next(200);
                    if (CountSwarm() < MaxSwarm)
                    {
                        mode = Mode.Bloat;
                        modeTimer = 0;
                        NPC.netUpdate = true;
                    }
                }
            }
        }

        void RunDartWindup(Player player)
        {
            modeTimer++;
            //Hovering tense: a tight, aggressive wiggle replaces the lazy drift
            NPC.velocity *= 0.8f;
            NPC.velocity += new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.6f, 0.6f));
            if (modeTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.35f, Pitch = 0.9f }, NPC.Center); //rising whine
            }
            if (Main.rand.NextBool(2))
            {
                int glint = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.HallowSpray, 0f, 0f, 60, default, 0.9f);
                Main.dust[glint].noGravity = true;
            }
            if (modeTimer >= DartWindupTicks)
            {
                NPC.velocity = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * DartSpeed;
                mode = Mode.Darting;
                modeTimer = 0;
                NPC.netUpdate = true;
            }
        }

        void RunDarting(Player player)
        {
            modeTimer++;
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.HallowSpray, 0f, 0f, 80, default, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
            //Latch on contact
            if (!player.dead && player.active && NPC.Hitbox.Intersects(player.Hitbox))
            {
                mode = Mode.Attached;
                modeTimer = 0;
                attachSide = NPC.Center.X > player.Center.X ? 1 : -1;
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = 0.8f }, NPC.Center);
                NPC.netUpdate = true;
                return;
            }
            //Missed, or smacked into a wall
            if (modeTimer >= DartMaxTicks || NPC.collideX || NPC.collideY)
            {
                mode = Mode.Drift;
                modeTimer = 0;
                dartCooldown = 180 + Main.rand.Next(120);
                NPC.netUpdate = true;
            }
        }

        void RunAttached(Player player)
        {
            modeTimer++;
            if (!player.active || player.dead)
            {
                Detach(120);
                return;
            }
            //Ride the player on its latched world-side. Front/back is judged against the player's
            //CURRENT facing — turning around brings a back-latcher into swing range.
            NPC.Center = player.Center + new Vector2(attachSide * 12f, -4f);
            NPC.velocity = Vector2.Zero;
            NPC.spriteDirection = -attachSide; //facing into the player, feeding
            bool onBack = attachSide != player.direction;
            NPC.dontTakeDamage = onBack; //back latch: only the dodge roll saves you

            //The bite: Bleeding, plus Poison in SHM
            if (modeTimer % 30 == 0)
            {
                player.AddBuff(BuffID.Bleeding, 120);
                if (SHM)
                {
                    player.AddBuff(BuffID.Poisoned, 120);
                }
            }
            if (Main.rand.NextBool(4))
            {
                int blood = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, attachSide, 0.5f, 60, default, 0.9f);
                Main.dust[blood].velocity *= 0.4f;
            }

            //Shaken off by the dodge roll, or it drinks its fill and leaves
            if (player.GetModPlayer<tsorcRevampPlayer>().isDodging)
            {
                Detach(240);
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = 1f }, NPC.Center);
            }
            else if (modeTimer >= MaxLatchTicks)
            {
                Detach(300);
            }
        }

        void Detach(int cooldown)
        {
            mode = Mode.Drift;
            modeTimer = 0;
            dartCooldown = cooldown;
            NPC.dontTakeDamage = false;
            NPC.velocity = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, -3f)); //tumbles away
            NPC.netUpdate = true;
        }

        void RunBloat()
        {
            modeTimer++;
            NPC.velocity *= 0.9f;
            //Swelling up to split: the whole point is that you can SEE it and stop it
            NPC.scale = 1f + 0.4f * (modeTimer / (float)BloatTicks);
            if (modeTimer == 1 || modeTimer == BloatTicks / 2)
            {
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.4f, Pitch = 0.6f + modeTimer / (float)BloatTicks * 0.4f }, NPC.Center);
            }
            //Sparkles condensing onto the swelling body
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = NPC.Center + angle.ToRotationVector2() * Main.rand.NextFloat(10f, 24f);
                int dust = Dust.NewDust(pos, 4, 4, DustID.HallowSpray, 0f, 0f, 60, default, 0.9f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (NPC.Center - pos) * 0.15f;
            }

            if (modeTimer >= BloatTicks)
            {
                //Pop: the copy tumbles out
                NPC.scale = 1f;
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.6f, Pitch = 1f }, NPC.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.HallowSpray, vel.X, vel.Y, 60, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && CountSwarm() < MaxSwarm)
                {
                    int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Parasprite>());
                    if (spawned < Main.maxNPCs)
                    {
                        Main.npc[spawned].velocity = -NPC.velocity * 2f + Main.rand.NextVector2Circular(3f, 3f);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
                        }
                    }
                }
                mode = Mode.Drift;
                modeTimer = 0;
                NPC.netUpdate = true;
            }
        }

        int CountSwarm()
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Parasprite>())
                {
                    count++;
                }
            }
            return count;
        }

        int CountAttached()
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Parasprite>()
                    && Main.npc[i].ModNPC is Parasprite sprite && sprite.IsAttached)
                {
                    count++;
                }
            }
            return count;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.HallowSpray, hit.HitDirection, -0.5f, 80, default, 0.9f);
                Main.dust[dust].noGravity = true;
            }
            if (NPC.life <= 0)
            {
                //Death poof — extra confetti if it was mid-bloat (the denied split escaping)
                int puffs = mode == Mode.Bloat ? 24 : 12;
                for (int i = 0; i < puffs; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.HallowSpray, vel.X, vel.Y, 50, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
                //Panic the swarm: neighbors scatter when one of their own dies
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].whoAmI != NPC.whoAmI && Main.npc[i].type == ModContent.NPCType<Parasprite>()
                        && Main.npc[i].ModNPC is Parasprite sprite && Vector2.Distance(Main.npc[i].Center, NPC.Center) < 128f)
                    {
                        sprite.panicTimer = 60;
                    }
                }
            }
        }

        ///<summary>Sheet rows: color's row + the one below it, flapped every 2 ticks.</summary>
        public override void FindFrame(int frameHeight)
        {
            int color = spriteColor > 0 ? spriteColor : 1; //fallback before the server's first sync
            NPC.frameCounter++;
            if (NPC.frameCounter == 2)
            {
                NPC.frame.Y = color * frameHeight;
            }
            if (NPC.frameCounter >= 4)
            {
                NPC.frame.Y = (color + 1) * frameHeight;
                NPC.frameCounter = 0;
            }
            if (mode != Mode.Attached)
            {
                if (NPC.velocity.X < 0) { NPC.spriteDirection = -1; }
                if (NPC.velocity.X > 0) { NPC.spriteDirection = 1; }
            }
        }
    }
}
