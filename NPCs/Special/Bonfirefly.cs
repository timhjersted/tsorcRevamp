using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Special 
{
    public class Bonfirefly : ModNPC 
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
        }
        public override void SetDefaults() 
        {
            NPC.width = 20;
            NPC.height = 20;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.friendly = false;
            NPC.lifeMax = 5;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
        }

        public override void AI() 
        {

            int h = (Math.Sign(NPC.position.X - NPC.oldPos[0].X) * 2) - 1;
            NPC.spriteDirection = h;
            NPC.oldPos[0] = NPC.position;


            bool homeBonfireLit  = Framing.GetTileSafely((int)NPC.ai[0], (int)NPC.ai[1]).TileFrameY >= 74;
            if (!homeBonfireLit) {
                //orbit around the unlit fire
                Vector2 homePos = new Vector2(((int)NPC.ai[0] + 1) * 16, ((int)NPC.ai[1] + 2) * 16);
                NPC.position = homePos + new Vector2(2, -16).RotatedBy(Math.Sin(Main.GlobalTimeWrappedHourly / 2));
            }
            else 
            {
                //run awaaaaaaay
                if (NPC.ai[2] > 0) {
                    NPC.ai[2] = -1;
                    SoundStyle flee = new("tsorcRevamp/Sounds/Custom/BonfireflyFlee") 
                    {
                        PitchRange = (-0.1f, 0.3f),
                        PlayOnlyIfFocused = true,
                        Volume = 0.004f * ModContent.GetInstance<tsorcRevampConfig>().BonfireFlyVolume, //Default is 100
                    };
                    SoundEngine.PlaySound(flee, NPC.position);
                }
                NPC.velocity.X = 0.5f * NPC.spriteDirection;
                NPC.velocity.Y -= 0.1f;
            }


            if (homeBonfireLit)
                return;

            NPC.ai[2]++;

            if (NPC.ai[2] >= 180 && Main.rand.NextBool(60)) 
            {
                SoundStyle twinkle = new("tsorcRevamp/Sounds/Custom/Bonfirefly") 
                {
                    PitchRange = (-0.1f, 0.3f),
                    PlayOnlyIfFocused = true,
                    Volume = 0.0085f * ModContent.GetInstance<tsorcRevampConfig>().BonfireFlyVolume, //Default is 100
                };
                SoundEngine.PlaySound(twinkle, NPC.position);
                NPC.ai[2] = 0;
            }
        }

        public override void FindFrame(int frameHeight) 
        {
            NPC.frameCounter++;
            if (NPC.frameCounter == 2) {
                NPC.frame.Y = 0;
            }
            if (NPC.frameCounter == 4) {
                NPC.frame.Y = 1 * frameHeight;
                NPC.frameCounter = 0;
            }
        }
    }
}
