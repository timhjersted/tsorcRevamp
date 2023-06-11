using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Marilith;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    class PrimeIntro : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 60;
            NPC.defense = 38;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 300000;
            NPC.timeLeft = 22500;
            NPC.alpha = 100;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.value = 600000;
            NPC.dontTakeDamage = true;
            NPC.behindTiles = true;
        }


        float progress = 0;
        bool tripped = false;
        public override void AI()
        {
            if(Main.GameUpdateCount % 180 == 0)
            {
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.1f, Pitch = Main.rand.NextFloat(-0.2f, 0.2f) }, NPC.Center);
            }


            if (Main.tile[5000, 1106].IsActuated)
            {
                PrimeV2.ActuateBottomHalf();
            }

            if (Main.tile[5080, 1100].TileType == TileID.Glass)
            {
                PrimeV2.ActuatePrimeArena();
            }

            NPC.Center = PrimeV2.PrimeCeilingPoint + new Vector2(0, -200);
            
            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].Distance(NPC.Center) < 550 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<PrimeV2>());
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
            }
        }


        public static Texture2D texture;
        public static Texture2D eyeTexture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeV2");
            UsefulFunctions.EnsureLoaded(ref eyeTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/Bone_Eyes");

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height / 6);
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, 0, drawOrigin, 1f, SpriteEffects.None, 0);

            Rectangle eyeRectangle = new Rectangle(0, 0, eyeTexture.Width, eyeTexture.Height / 3);
            Vector2 eyeOrigin = new Vector2(eyeRectangle.Width * 0.5f, eyeRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(eyeTexture, NPC.Center - Main.screenPosition - new Vector2(-2, 1), eyeRectangle, Color.White, 0, eyeOrigin, 1f, SpriteEffects.None, 0);

            return false;
        }
        
        public override bool CheckActive()
        {
            return false;
        }
    }
}