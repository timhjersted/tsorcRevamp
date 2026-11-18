using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Ornsmough
{
    /// <summary>
    /// The idol the altar wakes in the intro: a scripted stand-in drawn from Golem's vanilla textures. It is never struck.
    /// The director removes it silently, so downedGolemBoss, the death message and every loot rule stay untouched.
    /// </summary>
    public class CutsceneGolem : ModNPC
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Golem;

        private const int HeadType = NPCID.GolemHead;
        private const int LeftFistType = NPCID.GolemFistLeft;
        private const int RightFistType = NPCID.GolemFistRight;

        // Vanilla Golem's part spawn offsets from the body centre (NPC.AI_045): these are each part's bottom-centre.
        private static readonly Vector2 HeadOffset = new Vector2(-3f, -57f);
        private static readonly Vector2 LeftFistOffset = new Vector2(-84f, -9f);
        private static readonly Vector2 RightFistOffset = new Vector2(78f, -9f);

        private const int WakeTicks = 40;
        private const int SecondHopTick = 95;
        private const float HopUpSpeed = 5.25f;   // 35t airtime at gravity 0.3
        private const float HopForwardSpeed = 2.3f; // about 5 tiles per hop

        private ref float ScriptTicks => ref NPC.ai[0];
        private bool wasAirborne;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Golem];
            NPCID.Sets.NPCBestiaryDrawModifiers hidden = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = hidden;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 140;
            NPC.height = 140;
            NPC.aiStyle = -1;
            NPC.lifeMax = 39000;
            NPC.damage = 0;
            NPC.defense = 26;
            NPC.value = 0f;
            NPC.boss = true;
            NPC.knockBackResist = 0f;
            NPC.dontTakeDamage = true;
            NPC.lavaImmune = true;
            NPC.npcSlots = 0f;
        }

        public override void BossHeadSlot(ref int index)
        {
            index = NPCID.Sets.BossHeadTextures[NPCID.Golem];
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            ScriptTicks++;
            NPC.damage = 0;

            bool grounded = NPC.velocity.Y == 0f;
            if (grounded)
            {
                NPC.velocity.X = 0f;
            }

            if (grounded && wasAirborne)
            {
                PlayLanding();
            }
            wasAirborne = !grounded;

            KindleEyes();

            bool hopTick = ScriptTicks == WakeTicks || ScriptTicks == SecondHopTick;
            if (hopTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.TargetClosest(true);
                NPC.velocity = new Vector2(NPC.direction * HopForwardSpeed, -HopUpSpeed);
                NPC.netUpdate = true;
            }
        }

        private void KindleEyes()
        {
            if (Main.dedServ || ScriptTicks > WakeTicks)
            {
                return;
            }

            if (ScriptTicks == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
            }

            Vector2 eyes = NPC.Center + HeadOffset + new Vector2(0f, -30f);
            Vector2 sparkPoint = eyes + Main.rand.NextVector2Circular(16f, 6f);
            Dust ember = Dust.NewDustPerfect(sparkPoint, DustID.OrangeTorch, Main.rand.NextVector2Circular(0.8f, 0.8f), 0, default, 1.5f);
            ember.noGravity = true;
        }

        private void PlayLanding()
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.DeerclopsStep, NPC.Bottom);
            UsefulFunctions.ScreenShake(NPC.Bottom, 4f, 14, distanceFalloff: 1800f);

            for (int i = 0; i < 24; i++)
            {
                Vector2 dustPoint = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - 2f);
                Dust.NewDustPerfect(dustPoint, DustID.Lihzahrd, new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, -0.5f)), 0, default, 1.2f);
            }
        }

        // The body draws from this NPC's frame; head and fists are composed on the same centre with their own sheets.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Main.instance.LoadNPC(NPCID.Golem);
            Main.instance.LoadNPC(HeadType);
            Main.instance.LoadNPC(LeftFistType);
            Main.instance.LoadNPC(RightFistType);

            Vector2 bodyCenter = NPC.Center + new Vector2(0f, NPC.gfxOffY);
            DrawPartByBottom(spriteBatch, LeftFistType, bodyCenter + LeftFistOffset, screenPos, drawColor);
            DrawPartByBottom(spriteBatch, RightFistType, bodyCenter + RightFistOffset, screenPos, drawColor);

            Texture2D bodyTexture = TextureAssets.Npc[NPCID.Golem].Value;
            int bodyFrameHeight = bodyTexture.Height / Math.Max(1, Main.npcFrameCount[NPCID.Golem]);
            Rectangle bodyFrame = new Rectangle(0, 0, bodyTexture.Width, bodyFrameHeight);
            spriteBatch.Draw(bodyTexture, bodyCenter - screenPos, bodyFrame, drawColor, 0f, bodyFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            DrawPartByBottom(spriteBatch, HeadType, bodyCenter + HeadOffset, screenPos, drawColor);
            return false;
        }

        private static void DrawPartByBottom(SpriteBatch spriteBatch, int npcType, Vector2 bottomCenter, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[npcType].Value;
            int frameHeight = texture.Height / Math.Max(1, Main.npcFrameCount[npcType]);
            Rectangle frame = new Rectangle(0, 0, texture.Width, frameHeight);
            Vector2 origin = new Vector2(frame.Width * 0.5f, frame.Height);
            spriteBatch.Draw(texture, bottomCenter - screenPos, frame, drawColor, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}
