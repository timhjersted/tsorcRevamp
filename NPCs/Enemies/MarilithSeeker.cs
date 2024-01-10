using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class MarilithSeeker : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.width = 10;
            NPC.height = 10;
            NPC.aiStyle = -1;
            NPC.damage = 30;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.HitSound = null;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.DeathSound = SoundID.NPCDeath3;
            NPC.timeLeft = 600;
        }
        public override void AI()
        {
            NPC.velocity.X = NPC.ai[1];
            NPC.velocity.Y = NPC.ai[2];

            if (NPC.Center.Y > Main.player[NPC.target].position.Y + 5 || NPC.timeLeft < 450)
            {
                if (Collision.SolidCollision(NPC.position, NPC.width * 2, NPC.height * 2))
                {
                    for (int i = 0; i < 20; i++)
                    {
                        int spawnDust = Dust.NewDust(new Vector2(NPC.position.X - 6, NPC.position.Y - 6), NPC.width + 6, NPC.height + 6, DustID.InfernoFork, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), default, default, 1.8f);
                        Main.dust[spawnDust].velocity += NPC.velocity;
                        Main.dust[spawnDust].noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath3, NPC.Center);
                    NPC.active = false; //We bypass OnKill, because that might spawn a stamina drop upon hitting a tile. This way it only spawns those if the player actually hits it
                }
            }

            for (int j = 0; j < 2; j++)
            {
                int trailDust = Dust.NewDust(new Vector2(NPC.position.X - 6, NPC.position.Y - 6), NPC.width + 6, NPC.height + 6, DustID.Torch, NPC.velocity.X * 0.1f, NPC.velocity.Y * 0.1f, default, default, 1.3f);
                Main.dust[trailDust].velocity *= 0.3f;
                Main.dust[trailDust].noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return base.PreDraw(spriteBatch, screenPos, Color.White);
        }

        public override void OnKill()
        {
            for (int i = 0; i < 20; i++)
            {
                int spawnDust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y + 2f), NPC.width, NPC.height, DustID.InfernoFork, Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2), default, default, 1.8f);
                Main.dust[spawnDust].velocity += NPC.velocity;
                Main.dust[spawnDust].noGravity = true;
            }
        }
    }
}
