using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class ViciousSpit : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 16;
            NPC.height = 16;
            NPC.aiStyle = -1;
            NPC.damage = 115;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.HitSound = null;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.DeathSound = SoundID.NPCDeath9;
            NPC.alpha = 80;
            NPC.timeLeft = 600;
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        public override void SetStaticDefaults() {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }

        public override void AI()
        {
            if (NPC.target == 255)
            {
                NPC.TargetClosest();
                //Vector2 shotOrigin = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                //float distX = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - shotOrigin.X;
                //float distY = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - shotOrigin.Y;
                //float distAbs = (float)Math.Sqrt(distX * distX + distY * distY);
                //distAbs = 7f / distAbs;
                //npc.velocity.X = distX * distAbs;
                //npc.velocity.Y = distY * distAbs;
            }
            NPC.ai[0] += 1f;
            if (NPC.ai[0] > 3f)
            {
                NPC.ai[0] = 3f;
            }
            if (NPC.ai[0] == 2f)
            {
                NPC.position += NPC.velocity;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath9, NPC.Center);
                for (int i = 0; i < 20; i++)
                {
                    int spawnDust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y + 2f), NPC.width, NPC.height, 26, 0f, 0f, 100, default, 1.8f);
                    Main.dust[spawnDust].velocity += NPC.velocity;
                    Main.dust[spawnDust].noGravity = true;
                }
            }

            if (Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
            {
                _ = Main.netMode;
                NPC.StrikeNPCNoInteraction(999, 0f, 0);
            }
            for (int j = 0; j < 2; j++)
            {
                int trailDust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y + 2f), NPC.width, NPC.height, 26, NPC.velocity.X * 0.1f, NPC.velocity.Y * 0.1f, 80, default, 1.3f);
                Main.dust[trailDust].velocity *= 0.3f;
                Main.dust[trailDust].noGravity = true;
            }
        }
    }
}
