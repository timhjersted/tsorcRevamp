using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Tools;

namespace tsorcRevamp.NPCs.Enemies
{
    public class GhostOfTheForgottenWarrior : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }
        public int warriorDamage = 20;
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 35;
            NPC.lifeMax = 100;
            NPC.defense = 16;
            NPC.value = 500; // was 35
            NPC.width = 20;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.0f;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenWarriorBanner>();

            AnimationType = NPCID.GoblinWarrior;
            if (Main.hardMode)
            {
                NPC.lifeMax = 200;
                NPC.defense = 40;
                NPC.value = 1000; // was 45 but 30 defense
                NPC.damage = 60;
                topSpeed = 1.1f;
                warriorDamage = 30;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 90;
                NPC.damage = 90;
                NPC.value = 4000; // was 100 but worse stats
                topSpeed = 1.8f;
                warriorDamage = 50; // didn't have scaling damage before
            }

            UsefulFunctions.AddAttack(NPC, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), warriorDamage, 8, SoundID.Item17);
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if (!Main.hardMode && NPC.downedBoss3 && spawnInfo.Player.ZoneDungeon)
            {
                return 0.25f;
            }
            else if (Main.hardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.12f;
            }
            else if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.1f; //.05 is 3.85%
            }
            if (spawnInfo.Player.ZoneGraveyard)
            {
                return 0.2f; // was 0.17
            }
            return chance;
        }

        #endregion


        float topSpeed = 0.8f;

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, topSpeed, .04f, 0.2f, false, enragePercent: 0.2f, enrageTopSpeed: 2.1f); // experimenting with no longer teleporting

            if (NPC.justHit && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer <= 149 && Main.rand.NextBool(4))
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0f;
                NPC.netUpdate = true;
            }
        }

        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackKnightGhostSpear");
            }
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 150)
            {
                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                }
                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
                spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, spearTexture.Size() / 2, 1, SpriteEffects.None, 0);
            }
        }
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 3").Type, 1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ItemID.GoldenKey, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralDust>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<Items.Weapons.Throwing.EphemeralThrowingSpear>(), 10, 15, 26));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<GreatMagicShieldScroll>(), 30));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight));
            npcLoot.Add(hmCondition);
        }
    }
}