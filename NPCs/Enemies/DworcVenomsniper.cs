using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcVenomsniper : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
        }
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29; //spider
            NPC.DeathSound = SoundID.NPCDeath29;//lizard
            NPC.damage = 26;
            NPC.lifeMax = 50;
            NPC.defense = 8;
            NPC.value = 250; // was 37
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.1f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVenomsniperBanner>();

            AnimationType = NPCID.Skeleton;
            Main.npcFrameCount[NPC.type] = 15;
            UsefulFunctions.AddAttack(NPC, 180, ModContent.ProjectileType<Projectiles.Enemy.ArcherBolt>(), 9, 8, SoundID.Item63, telegraphColor: Color.GreenYellow);
        }

        //these mfs drop Every Potion too 
        //why : no more! now warlock has them
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {     
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 2, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 5, 1, 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 7));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 5));
        }

        //Spawns in the Jungle, mostly Underground and in the Cavern.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;
            if (spawnInfo.Water) return 0f;

            if (spawnInfo.Player.ZoneDungeon)
            {
                return 0f;
            }
            else if (!Main.hardMode && spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneOverworldHeight)
            {
                return 0.2f;
            }
            else if (!Main.hardMode && spawnInfo.Player.ZoneJungle && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
            {
                return 0.345f;
            }

            return chance;
        }

        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.2f, 0.05f, canPounce: false);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTelegraphStart)
            {
                Texture2D blowpipeTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/DworcVenomsniper_Telegraph");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(blowpipeTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 56), drawColor, NPC.rotation, new Vector2(22, 32), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(blowpipeTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 56), drawColor, NPC.rotation, new Vector2(22, 32), NPC.scale, effects, 0);
                }
            }
        }

        #region Gore
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}
