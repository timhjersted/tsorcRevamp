using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
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
            NPC.damage = 38;
            NPC.defense = 35;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 12500;
            NPC.knockBackResist = 0.1f;
            AnimationType = 28;
            Main.npcFrameCount[NPC.type] = 16;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.GhostOfTheDarkmoonKnightBanner>();
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
            if (Main.rand.Next(4) == 0)
            {
                target.AddBuff(BuffID.Bleeding, 300);
                target.AddBuff(BuffID.Poisoned, 300);
                target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800);
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Darkmoon Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 3").Type, 1f);
            }
        }

        public override void OnKill()
        {

            if (Main.rand.Next(20) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.GigantAxe>(), 1, false, -1);
            if (Main.rand.Next(50) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.FlaskofFire); //was firesoul
            if (Main.rand.Next(50) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
            if (Main.rand.Next(20) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 3, 0.175f, 0.2f, true, enragePercent: 0.1f, enrageTopSpeed: 6);

            bool canFire = NPC.Distance(Main.player[NPC.target].Center) < 500 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref shadowShotTimer, 170, ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(), 20, 9, canFire, true, 2, 17, 0);

            if (NPC.justHit)
            {
                shadowShotTimer = 100f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.Next(400) == 1)
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
            int spriteHeight = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];

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
                spearTexture = Mod.GetTexture("Projectiles/Enemy/ShadowShot");
            }
            if (shadowShotTimer >= 150)
            {
                Lighting.AddLight(NPC.Center, Color.MediumPurple.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(3) == 1)
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
