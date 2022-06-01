using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class FreedFairy : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Freed Fairy");
            Main.npcFrameCount[NPC.type] = 6;
            //NPCID.Sets.HatOffsetY[npc.type] = 4;
        }
        public override string TownNPCName()
        {
            return "Freed Fairy";
        }
        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.noGravity = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 18;
            NPC.aiStyle = 7;
            NPC.damage = 90;
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            NPC.dontTakeDamage = true;

        }

        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = ((Texture2D)TextureAssets.Npc[NPC.type]).Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;

            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
        #endregion

        #region Chat
        public override string GetChat()
        {
            return "Thank you for freeing me! Have the Oxyale I recovered from the bottom of the spring";
        }
        #endregion
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("Accept");
        }

        bool droppedOxyale = false;
        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            if (!droppedOxyale)
            {
                //Drop one for each player
                for (int i = 0; i < Main.ActivePlayersCount; i++)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Oxyale>());
                }
                droppedOxyale = true;
            }
        }

        #region AI
        public override void AI()
        {
            //The despawning has to be done in AI(), because spawning dusts from within OnChatButtonClicked doesn't work specifically when autopause is on.
            //I don't know why. I probably don't want to know why. This is as simple as workarounds get, though.
            if (droppedOxyale)
            {
                NPC.active = false;
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.BlueFairy, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3));
                }
                return;
            }
            NPC.TargetClosest(true);

            this.NPC.directionY = -1;
            if (this.NPC.direction == 0)
            {
                this.NPC.direction = 1;
            }

            if (Main.player[NPC.target].active && Main.player[NPC.target].talkNPC == this.NPC.whoAmI)
            {
                if (this.NPC.ai[0] != 0f)
                {
                    this.NPC.netUpdate = true;
                }
                this.NPC.ai[0] = 0f;
                this.NPC.ai[1] = 300f;
                this.NPC.ai[2] = 100f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) < this.NPC.position.X + (float)(this.NPC.width / 2))
                {
                    this.NPC.direction = -1;
                }
                else
                {
                    this.NPC.direction = 1;
                }
            }

            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }
                NPC.TargetClosest(true);
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }
            int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num260 = (int)((NPC.position.Y + (float)NPC.height) / 16f);
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y -= .2f;
                if (NPC.velocity.Y < -1.8f)
                {
                    NPC.velocity.Y = -1.8f;
                }
            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += .2f;
                if (NPC.velocity.Y > 1.8f)
                {
                    NPC.velocity.Y = 1.8f;
                }
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float num270 = 2.5f;
            if (NPC.direction == -1 && NPC.velocity.X > -num270)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > num270)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;
                    }
                }
                if (NPC.velocity.X < -num270)
                {
                    NPC.velocity.X = -num270;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < num270)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -num270)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > num270)
                    {
                        NPC.velocity.X = num270;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -2.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -2.5)
                {
                    NPC.velocity.Y = -2.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -2.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 2.5)
                    {
                        NPC.velocity.Y = 2.5f;
                    }
                }
            }

            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.6f, 0.3f, 0.0f);
            return;
        }
        #endregion
    }
}