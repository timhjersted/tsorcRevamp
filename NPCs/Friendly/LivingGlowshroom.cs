using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Friendly
{
    public class LivingGlowshroom : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Living Glowshroom");
            Main.npcFrameCount[NPC.type] = 8;
        }

        public override void SetDefaults()
        {
            NPC.width = 8;
            NPC.height = 18;
            NPC.aiStyle = -1; //Unique AI is -1
            NPC.damage = 0;
            NPC.knockBackResist = 1;
            NPC.defense = 4;
            NPC.lifeMax = 28;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 0;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.noGravity = false;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.LivingGlowshroomBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;

            if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].TileType == TileID.MushroomGrass)
            {
                return 0.75f;
            }
            return chance;
        }

        private const int AI_State_Slot = 0;
        private const int AI_Timer_Slot = 1;

        private const int State_Asleep = 0;
        //private const int State_Notice = 1; not used
        private const int State_Jump = 2;
        private const int State_Fleeing = 3;

        public float AI_State
        {
            get => NPC.ai[AI_State_Slot];
            set => NPC.ai[AI_State_Slot] = value;
        }

        public float AI_Timer
        {
            get => NPC.ai[AI_Timer_Slot];
            set => NPC.ai[AI_Timer_Slot] = value;
        }

        public int spawntimer = 0;

        // Our AI here makes our NPC sit waiting for a player to enter range then spawns minions to attack.
        public override void AI()
        {
            if ((NPC.life < NPC.lifeMax) && (Main.rand.NextBool(2)) && Framing.GetTileSafely((int)NPC.position.X / 16, (int)NPC.position.Y / 16 + 2).TileType == TileID.MushroomGrass)
            {
                Dust.NewDust(NPC.position, NPC.width - 6, NPC.height - 16, 107, 0, 0, 0, default(Color), .75f); //regenerating hp
            }
            Lighting.AddLight(NPC.Center, .12f, .12f, .5f);
            // The npc starts in the asleep state, waiting for a player to enter range
            if (AI_State == State_Asleep)
            {
                NPC.GivenName = "???";
                // TargetClosest sets npc.target to the player.whoAmI of the closest player. the faceTarget parameter means that npc.direction will automatically be 1 or -1 if the targeted player is to the right or left. This is also automatically flipped if npc.confused
                NPC.TargetClosest(true);
                AI_Timer++;
                if (NPC.velocity.Y == 0)
                {
                    NPC.velocity = new Vector2(0f, 0f);
                }
                if (NPC.HasValidTarget && Main.player[NPC.target].Distance(NPC.Center) < 120f)
                {
                    AI_State = State_Jump;
                    AI_Timer = 0;
                }
                if ((NPC.life < NPC.lifeMax) && (Main.rand.NextBool(8)))
                {
                    Dust.NewDust(NPC.position, NPC.width - 6, NPC.height - 16, 107, 0, 0, 0, default(Color), .75f); //regenerating hp
                }

                // if hit, change to flee state
            }
            else if (AI_State == State_Jump)
            {
                NPC.GivenName = "Fleeing Fungi";
                AI_Timer++;
                if (AI_Timer == 1)
                {
                    NPC.velocity = new Vector2(NPC.direction * -2.2f, -3.6f);
                }
                if ((Main.rand.NextBool(8)) && (AI_Timer == 2) && NPC.collideX /*&& Main.netMode != NetmodeID.MultiplayerClient*/)
                {
                    if (NPC.direction == -1) //right-facing bump
                    {
                        NPC.velocity += new Vector2(-1f, 0);
                        //if (!Main.dedServ) Terraria.Audio.SoundEngine.PlaySound(mod.GetLegacySoundSlot(SoundType.NPCHit, "tsorcRevamp/Sounds/NPCHit/Squeak") with { Volume = 0.5f }, npc.Center);
                        NPC.netUpdate = true;
                    }
                    if (NPC.direction == 1) //left-facing bump
                    {
                        NPC.velocity += new Vector2(1f, 0);
                        //if (!Main.dedServ) Terraria.Audio.SoundEngine.PlaySound(mod.GetLegacySoundSlot(SoundType.NPCHit, "tsorcRevamp/Sounds/NPCHit/Squeak") with { Volume = 0.5f }, npc.Center);
                        NPC.netUpdate = true;
                    }
                }
                else if (AI_Timer == 10)
                {
                    AI_State = State_Fleeing;
                    AI_Timer = 0;
                }
                NPC.netUpdate = true;
            }
            else if (AI_State == State_Fleeing) //everything is inverted due to npc running away from the player instead of towards. Sprite is also manually mirrored (the png, not codewise)
            {
                NPC.TargetClosest(true);
                if (NPC.direction == 1) //FACING LEFT - vel to move left
                {
                    if (NPC.velocity.X > -2.5f)
                    {
                        NPC.velocity += new Vector2(-.05f, 0); //breaking power after turn
                    }
                    else if (NPC.velocity.X < -3.6f) //max vel
                    {
                        NPC.velocity += new Vector2(.04f, 0); //slowdown after knockback
                    }
                    else if ((NPC.velocity.X <= -2.5f) && (NPC.velocity.X > -3.6f))
                    {
                        NPC.velocity += new Vector2(-.02f, 0); //running accel.
                    }
                }

                if (NPC.direction == -1) //FACING RIGHT + vel to move right
                {
                    if (NPC.velocity.X < 2.5f)
                    {
                        NPC.velocity += new Vector2(.05f, 0); //breaking power
                    }
                    else if (NPC.velocity.X > 3.6f) //max vel
                    {
                        NPC.velocity += new Vector2(-.04f, 0); //slowdown after knockback
                    }
                    else if ((NPC.velocity.X >= 2.5f) && (NPC.velocity.X < 3.6f))
                    {
                        NPC.velocity += new Vector2(.02f, 0); //running accel.
                    }
                }
                if (NPC.collideX)
                {
                    // NPC has stopped upon hitting a block
                    AI_State = State_Jump;
                    AI_Timer = 0;
                }
                if (!NPC.HasValidTarget || Main.player[NPC.target].Distance(NPC.Center) > 330f)
                {
                    // Out targeted player seems to have left our range, so we'll go back to sleep.
                    AI_State = State_Asleep;
                    AI_Timer = 0;
                }
                //if justbeenhittimer < 60, run
            }
        }

        private const int Frame_Asleep = 7;
        private const int Frame_Fleeing_0 = 0;
        private const int Frame_Fleeing_1 = 1;
        private const int Frame_Fleeing_2 = 2;
        private const int Frame_Fleeing_3 = 3;
        private const int Frame_Fleeing_4 = 4;
        private const int Frame_Fleeing_5 = 5;
        private const int Frame_Fleeing_6 = 6;

        public override void FindFrame(int frameHeight)
        {
            // This makes the sprite flip horizontally in conjunction with the npc.direction.


            // For the most part, our animation matches up with our states.
            if (AI_State == State_Asleep)
            {
                // npc.frame.Y is the goto way of changing animation frames. npc.frame starts from the top left corner in pixel coordinates, so keep that in mind.
                NPC.frame.Y = Frame_Asleep * frameHeight;

            }
            else if (AI_State == State_Jump)
            {
                NPC.frame.Y = Frame_Fleeing_5 * frameHeight;
            }
            else if (AI_State == State_Fleeing)
            {
                // Cycle through all 8 frames
                NPC.spriteDirection = NPC.direction;
                NPC.frameCounter++;
                if (NPC.frameCounter < 4)
                {
                    NPC.frame.Y = Frame_Fleeing_0 * frameHeight;
                }
                else if (NPC.frameCounter < 8)
                {
                    NPC.frame.Y = Frame_Fleeing_1 * frameHeight;
                }
                else if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = Frame_Fleeing_2 * frameHeight;
                }
                else if (NPC.frameCounter < 16)
                {
                    NPC.frame.Y = Frame_Fleeing_3 * frameHeight;
                }
                else if (NPC.frameCounter < 20)
                {
                    NPC.frame.Y = Frame_Fleeing_4 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = Frame_Fleeing_5 * frameHeight;
                }
                else if (NPC.frameCounter < 28)
                {
                    NPC.frame.Y = Frame_Fleeing_6 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }
        }
        public override void UpdateLifeRegen(ref int damage)
        {
            if (AI_State == State_Asleep)
            {
                NPC.lifeRegen = 2;
            }
            //if (Main.tile[(int)npc.position.X, (int)npc.position.Y].type == TileID.MushroomGrass)
            if (Framing.GetTileSafely((int)NPC.position.X / 16, (int)NPC.position.Y / 16 + 2).TileType == TileID.MushroomGrass)
            {
                NPC.lifeRegen = 15;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 15; i++)
            {
                int DustType = 41;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];

                dust.scale *= .70f + Main.rand.Next(-30, 31) * 0.01f;
                dust.velocity.Y = Main.rand.Next(-2, 0);
                dust.noGravity = false;
                dust.alpha = 120;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 41, 0, Main.rand.Next(-2, 0), 120, default(Color), .75f);
                }
            }
        }
        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GlowingMushroom);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 3);
        }
    }
}
