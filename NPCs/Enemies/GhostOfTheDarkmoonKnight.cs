using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs.Enemies
{
    class GhostOfTheDarkmoonKnight : ModNPC
    {


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }

        public override void SetDefaults()
        {
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 55;
            NPC.defense = 65;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 12000; // was 1250
            NPC.knockBackResist = 0.0f;
            AnimationType = 28;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheDarkmoonKnightBanner>();
            UsefulFunctions.AddAttack(NPC, 170, ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(), 20, 9, SoundID.Item17);
        }

        int chargeDamage = 0;
        bool charging = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool BeforeFourAfterSix = spawnInfo.SpawnTileX < Main.maxTilesX * 0.6f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.8f; //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?) but different because I increased numbers by .2

            if (tsorcRevampWorld.SuperHardMode && BeforeFourAfterSix && spawnInfo.Player.ZoneDungeon)
            {
                return 0.6f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneUnderworldHeight && spawnInfo.Player.ZoneDungeon)
            {
                return 0.5f;
            }
            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && spawnInfo.Player.ZoneDungeon)
            {
                return 0.4f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.2f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneHallow)
            {
                return 0.1f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneForest && !Main.dayTime)
            {
                return 0.3f;
            }

            return 0;
        }
        #endregion

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Slow, 600);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Poisoned, 60 * 60);
                target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Darkmoon Knight Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
                }
            }
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Axes.GigantAxe>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.StrengthPotion>(), 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofFire));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.ShockwavePotion>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 3));


        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.4f, 0.175f, 0.2f, false, enragePercent: 0.2f, enrageTopSpeed: 3, canDodgeroll: true);

            if (NPC.justHit && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer <= 140f && Main.rand.NextBool(4))
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 90f;
                NPC.netUpdate = true;
            }

            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 150 && Main.rand.NextBool(3))
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemDiamond, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemDiamond, NPC.velocity.X, NPC.velocity.Y);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.NextBool(400))
                {
                    charging = true;
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 10);
                    NPC.netUpdate = true;
                }
                if (charging == true)
                {
                    NPC.damage = 0;
                    chargeDamage++;
                    Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ichor, 0, 0).noGravity = true;
                }
                if (chargeDamage >= 90)
                {
                    charging = false;
                    NPC.damage = 70;
                    chargeDamage = 0;
                }
            }
        }

        static Texture2D spearTexture;
        static Texture2D darkKnightGlow;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            int spriteWidth = NPC.frame.Width; //use same number as ini frameCount
            int spriteHeight = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];

            int spritePosDifX = (int)(NPC.frame.Width / 2);
            int spritePosDifY = NPC.frame.Height - 5; // was npc.frame.Height - 4; if not 5 then 8

            int frame = NPC.frame.Y / spriteHeight;

            int offsetX = (int)(NPC.position.X + (NPC.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
            int offsetY = (int)(NPC.position.Y + NPC.height - Main.screenPosition.Y - spritePosDifY);

            SpriteEffects flop = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                flop = SpriteEffects.FlipHorizontally;
            }

            UsefulFunctions.EnsureLoaded(ref darkKnightGlow, "tsorcRevamp/Gores/Ghost of the Darkmoon Knight Glow");

            //Glowing Eye Effect
            for (int i = 1; i > -1; i--)
            {
                //draw 3 levels of trail
                int alphaVal = 255 - (1 * i);
                Color modifiedColour = new Color((int)(alphaVal), (int)(alphaVal), (int)(alphaVal), alphaVal);
                spriteBatch.Draw(darkKnightGlow,
                    new Rectangle((int)(offsetX), (int)(offsetY), spriteWidth, spriteHeight),
                    new Rectangle(0, NPC.frame.Height * frame, spriteWidth, spriteHeight),
                    modifiedColour,
                    NPC.rotation,  //Just add this here I think
                    new Vector2(0, 0),
                    flop,
                    0);
            }

            if (spearTexture == null)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/ShadowShot");
            }
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 150)
            {
                Lighting.AddLight(NPC.Center, Color.MediumPurple.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
                spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, spearTexture.Size() / 2, 1, SpriteEffects.None, 0);
            }
        }
    }
}
