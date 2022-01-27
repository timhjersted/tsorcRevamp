using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Accessories;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies {
    class GhostOfTheDarkmoonKnight : ModNPC {


        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ghost of the Darkmoon Knight");
        }

        public override void SetDefaults() {
            npc.height = 40;
            npc.width = 20;
            npc.damage = 38;
            npc.defense = 35;
            npc.lifeMax = 3000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 12500;
            npc.knockBackResist = 0.1f;
            animationType = 28;
            Main.npcFrameCount[npc.type] = 16;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.GhostOfTheDarkmoonKnightBanner>();
        }


        float shadowShotTimer;
        int chargeDamage = 0;
        bool charging = false;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool BeforeFourAfterSix = spawnInfo.spawnTileX < Main.maxTilesX * 0.6f || spawnInfo.spawnTileX > Main.maxTilesX * 0.8f; //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?) but different because I increased numbers by .2

            if (tsorcRevampWorld.SuperHardMode && BeforeFourAfterSix && spawnInfo.player.ZoneDungeon)
            {
                return 0.3f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneUnderworldHeight && spawnInfo.player.ZoneDungeon)
            {
                return 0.4f;
            }
            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && spawnInfo.player.ZoneDungeon)
            {
                return 0.2f; 
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon)
            {
                return 0.1f;
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon)
            {
                return 0.08f;
            }

            return 0;
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(4) == 0) {
                target.AddBuff(BuffID.Bleeding, 300);
                target.AddBuff(BuffID.Poisoned, 300);
                target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800);
            }
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (npc.life <= 0) {
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Darkmoon Knight Gore 1"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);
            }
        }

        public override void NPCLoot() {

            if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.GigantAxe>(), 1, false, -1);
            if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ItemID.FlaskofFire); //was firesoul
            if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
            if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
            if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 3, 0.175f, 0.2f, true, enragePercent: 0.1f, enrageTopSpeed: 6);

            bool canFire = npc.Distance(Main.player[npc.target].Center) < 500 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
            tsorcRevampAIs.SimpleProjectile(npc, ref shadowShotTimer, 170, ModContent.ProjectileType<Projectiles.Enemy.ShadowShot>(), 20, 9, canFire, true, 2, 17, 0);

            if (npc.justHit)
            {
                shadowShotTimer = 100f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.Next(400) == 1)
                {
                    charging = true;
                    npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 10);
                    npc.netUpdate = true;
                }
                if (charging == true)
                {
                    npc.damage = 120;
                    chargeDamage++;
                }
                if (chargeDamage >= 50)
                {
                    charging = false;
                    npc.damage = 70;
                    chargeDamage = 0;
                }
            }         
        }

        static Texture2D spearTexture;
        static Texture2D darkKnightGlow = ModContent.GetTexture("tsorcRevamp/Gores/Ghost of the Darkmoon Knight Glow");
        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor) {

            int spriteWidth = npc.frame.Width; //use same number as ini frameCount
            int spriteHeight = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];

            int spritePosDifX = (int)(npc.frame.Width / 2);
            int spritePosDifY = npc.frame.Height - 5; // was npc.frame.Height - 4; if not 5 then 8

            int frame = npc.frame.Y / spriteHeight;

            int offsetX = (int)(npc.position.X + (npc.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
            int offsetY = (int)(npc.position.Y + npc.height - Main.screenPosition.Y - spritePosDifY);

            SpriteEffects flop = SpriteEffects.None;
            if (npc.spriteDirection == 1) {
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
                    new Rectangle(0, npc.frame.Height * frame, spriteWidth, spriteHeight),
                    modifiedColour,
                    npc.rotation,  //Just add this here I think
                    new Vector2(0, 0),
                    flop,
                    0);
            }

            if (spearTexture == null)
            {
                spearTexture = mod.GetTexture("Projectiles/Enemy/ShadowShot");
            }
            if (shadowShotTimer >= 150)
            {
                Lighting.AddLight(npc.Center, Color.MediumPurple.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(3) == 1)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.DiamondBolt, npc.velocity.X, npc.velocity.Y);
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.DiamondBolt, npc.velocity.X, npc.velocity.Y);
                }

                SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (npc.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 10), npc.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 13), npc.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }

        }

    }
}
