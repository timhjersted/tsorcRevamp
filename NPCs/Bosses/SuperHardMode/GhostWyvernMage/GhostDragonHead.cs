using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    [AutoloadBossHead]
    class GhostDragonHead : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 45;
            npc.height = 45;
            drawOffsetY = drawOffset;
            npc.aiStyle = 6;
            npc.knockBackResist = 0;
            npc.timeLeft = 22750;
            npc.damage = 115;
            npc.defense = 120;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath10;
            npc.lifeMax = 760000;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.alpha = 100;
            npc.value = 660000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
            bossBag = ModContent.ItemType<Items.BossBags.GhostWyvernBag>();
            despawnHandler = new NPCDespawnHandler(DustID.OrangeTorch);
        }
        int lightningDamage = 40;

        public static int drawOffset = 52;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghost Wyvern");
        }

        NPCDespawnHandler despawnHandler;
        int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            npc.localAI[1]++;
            npc.localAI[2]++;
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, -2f, 15f, 0.23f, true, false, true, false, false);
            tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[2], 1500, ProjectileID.CultistBossLightningOrb, lightningDamage, 10, Main.rand.Next(200) == 1, false, 2, 17);
            tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 660, ProjectileID.FrostWave, lightningDamage, 1, Main.rand.Next(200) == 1, false, 2, 20);

            //this makes the head always stay in the same position even when it flips upside down
            if (npc.velocity.X < 0f) { npc.spriteDirection = 1; }
            else  //both -1 is correct
            if (npc.velocity.X > 0f) { npc.spriteDirection = -1; }

        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        public override void NPCLoot()
        {
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));            
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 5.0f);
            Main.dust[dust].noGravity = true;

            //Only drop the loot if the mage is already dead. If it's not, then he will drop it instead.
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
            {
                if (Main.expertMode)
                {
                    npc.DropBossBags();
                }
                else
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GhostWyvernSoul>(), 8);
                }
            } else
            {

                UsefulFunctions.BroadcastText("The souls of " + npc.GivenOrTypeName + " have been released!", 175, 255, 75);
                tsorcRevampWorld.Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()] = 1;
            }
        }


        public static Texture2D texture;
        public static void GhostEffect(NPC npc, SpriteBatch spriteBatch, ref Texture2D texture, float scale = 1.5f)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = ModContent.GetTexture(npc.modNPC.Texture);
            }
            
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer);
                data.Apply(null);
                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, sourceRectangle, Color.White, npc.rotation, origin, scale, effects, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            

        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            GhostEffect(npc, spriteBatch, ref texture, 1.5f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
           // GhostDragonHead.GhostEffect(npc, spriteBatch, ref texture, 1.5f);
        }



    }
}