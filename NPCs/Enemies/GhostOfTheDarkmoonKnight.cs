using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class GhostOfTheDarkmoonKnight : ModNPC
    {


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghost of the Darkmoon Knight");
        }

        public override void SetDefaults()
        {
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 45;
            NPC.defense = 65;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 12500;
            NPC.knockBackResist = 0.1f;
            AnimationType = 28;
            Main.npcFrameCount[NPC.type] = 16;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheDarkmoonKnightBanner>();
        }


        float shadowShotTimer;
        int chargeDamage = 0;
        bool charging = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool BeforeFourAfterSix = spawnInfo.SpawnTileX < Main.maxTilesX * 0.6f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.8f; //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?) but different because I increased numbers by .2

            if (tsorcRevampWorld.SuperHardMode && BeforeFourAfterSix && spawnInfo.Player.ZoneDungeon)
            {
                return 0.3f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneUnderworldHeight && spawnInfo.Player.ZoneDungeon)
            {
                return 0.4f;
            }
            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && spawnInfo.Player.ZoneDungeon)
            {
                return 0.2f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.1f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.08f;
            }

            return 0;
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Slow, 300);

            if (Main.rand.NextBool(4))
            {
                target.AddBuff(BuffID.Poisoned, 600);
                target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800);
            }
        }

        public override void HitEffect(int hitDirection, double damage)
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


        //why were this thing's drop rates so low?
        //they were all 2-5% in tconfig too. wtf
        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Broadswords.GigantAxe>(), 20));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 12));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.StrengthPotion>(), 12));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.FlaskofFire, 12));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.ShockwavePotion>(), 12));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 12));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.AttractionPotion>(), 20));


        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2, 0.175f, 0.2f, true, enragePercent: 0.1f, enrageTopSpeed: 5);

            bool canFire = NPC.Distance(Main.player[NPC.target].Center) < 500 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref shadowShotTimer, 170, ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(), 20, 9, canFire, true, SoundID.Item17, 0);

            if (NPC.justHit && shadowShotTimer < 120f && Main.rand.NextBool(3))
            {
                shadowShotTimer = 90f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.NextBool(400))
                {
                    charging = true;
                    NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 10);
                    NPC.netUpdate = true;
                }
                if (charging == true)
                {
                    NPC.damage = 120;
                    chargeDamage++;
                }
                if (chargeDamage >= 50)
                {
                    charging = false;
                    NPC.damage = 70;
                    chargeDamage = 0;
                }
            }
        }

        static Texture2D spearTexture;
        static Texture2D darkKnightGlow = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Gores/Ghost of the Darkmoon Knight Glow");
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
            if (shadowShotTimer >= 150)
            {
                Lighting.AddLight(NPC.Center, Color.MediumPurple.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemDiamond, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemDiamond, NPC.velocity.X, NPC.velocity.Y);
                }

                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 10), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 13), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }

        }

    }
}
