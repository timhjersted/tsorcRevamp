using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    class GhostDragonLegs : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 45;
            NPC.height = 45;
            DrawOffsetY = GhostDragonHead.drawOffset;
            NPC.aiStyle = 6;
            NPC.knockBackResist = 0;
            NPC.timeLeft = 750;
            NPC.damage = 30;
            NPC.defense = 50;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath10;
            NPC.lifeMax = 35000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = 10000;
            NPC.alpha = 100;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.damage = (int)(NPC.damage / 2);
        }
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ghost Wyvern");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void AI()
        {
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.behindTiles = true;
            int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, -2f, 15f, 0.23f, true, false);

            if (!Main.npc[(int)NPC.ai[1]].active)
            {
                NPC.life = 0;
                NPC.HitEffect(0, 10.0);
                NPC.active = false;
            }

            //if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X)
            //{
            //	npc.spriteDirection = 1;
            //}
            //if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
            //{
            //	npc.spriteDirection = -1;
            //}


        }


        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0)
            {

                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 17, 0, 0, 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-0, 1) * 0.2f, (float)Main.rand.Next(-0, 1) * 0.2f), Main.rand.Next(61, 64), 1f);
                }


            }
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            GhostDragonHead.GhostEffect(NPC, spriteBatch, ref texture, 1.5f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //GhostDragonHead.GhostEffect(npc, spriteBatch, ref texture, 1.2f);
        }
    }
}