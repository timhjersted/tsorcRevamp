using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class OolacileKnight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 2;
            AnimationType = 28;
            NPC.height = 40;
            NPC.width = 20;
            //Music = 12;
            NPC.damage = 63;
            NPC.defense = 70;
            NPC.lavaImmune = true;
            NPC.lifeMax = 9000;
            NPC.scale = 1.1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 28750;
            NPC.knockBackResist = 0f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.OolacileKnightBanner>();
            NPC.lavaImmune = true;
        }

        int dragonsBreathDamage = 39;
        int darkExplosionDamage = 37;
        int earthTridentDamage = 35;

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            dragonsBreathDamage = (int)(dragonsBreathDamage * tsorcRevampWorld.SHMScale);
            darkExplosionDamage = (int)(darkExplosionDamage * tsorcRevampWorld.SHMScale);
            earthTridentDamage = (int)(earthTridentDamage * tsorcRevampWorld.SHMScale);
        }



        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //this shortens our code up from writing this line over and over.

            bool Sky = spawnInfo.SpawnTileY <= (Main.rockLayer * 4);
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.SpawnTileY >= Main.worldSurface && spawnInfo.SpawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.SpawnTileY >= (Main.maxTilesY - 200) * 16;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            // these are all the regular stuff you get , now lets see......

            if (spawnInfo.Water) return 0f;

            if (NPC.AnyNPCs(ModContent.NPCType<OolacileKnight>()))
            {
                return 0;
            }

            if (Jungle && tsorcRevampWorld.SuperHardMode && AboveEarth && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<OolacileKnight>())) && Main.rand.NextBool(20))

            {
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.OolacileKnight.Intent"), 175, 75, 255);
                return 1;
            }

            if (Dungeon && Main.bloodMoon && tsorcRevampWorld.SuperHardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<OolacileKnight>())) && Main.rand.NextBool(15))

            {
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.OolacileKnight.Hunt"), 175, 75, 255);
                return 1;
            }

            if (Meteor && Main.bloodMoon && tsorcRevampWorld.SuperHardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<OolacileKnight>())) && Main.rand.NextBool(20))

            {
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.OolacileKnight.Hunt"), 175, 75, 255);
                return 1;
            }

            if (Dungeon && tsorcRevampWorld.SuperHardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<OolacileKnight>())) && Main.rand.NextBool(30))

            {
                UsefulFunctions.BroadcastText(LaUtils.GetTextValue("NPCs.OolacileKnight.Hunt"), 175, 75, 255);
                return 1;
            }

            return 0;
        }
        #endregion

        int hitCounter;
        float tridentTimer;
        float breathTimer;

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.03f, canTeleport: true, randomSound: SoundID.Mummy, soundFrequency: 2000, enragePercent: 0.5f, enrageTopSpeed: 4);
            tsorcRevampAIs.LeapAtPlayer(NPC, 7, 4, 1.5f, 128);
            tsorcRevampAIs.SimpleProjectile(NPC, ref tridentTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EarthTrident>(), earthTridentDamage, 11, Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height), true, SoundID.Item17);

            breathTimer++;
            if (breathTimer > 500)
            {
                breathTimer = -90;
            }

            if (breathTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 12);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), dragonsBreathDamage, 0f, Main.myPlayer);
                    NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                }
            }

            if (breathTimer > 360)
            {
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((500 - breathTimer) / 120)), DustID.Torch, 48, 4);
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
            }


            Player player = Main.player[NPC.target];
            //when close to enemy, grapple and mobility hindered
            if (NPC.Distance(player.Center) < 600)
            {
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 2);
            }
            if (Main.hardMode && NPC.Distance(player.Center) < 100)
            {
                player.AddBuff(ModContent.BuffType<Crippled>(), 60, false);
            }


            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (NPC.justHit)
                {
                    hitCounter++;
                }

                if (hitCounter > 6 || (NPC.life < NPC.lifeMax / 10 && Main.rand.NextBool(400)))
                {
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 15);
                    NPC.netUpdate = true;
                    hitCounter = 0;
                    for (int i = 0; i < 50; i++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, default, 2f);
                    }
                    for (int i = 0; i < 20; i++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 100, default, 2f);
                    }
                }

                if (Main.rand.NextBool(1000))
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.OolacileDemon>(), 0);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }
        }




        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 600 * 60, false); //-20 HP curse
            }

            if (Main.rand.NextBool(4))
            {

                target.AddBuff(BuffID.BrokenArmor, 10 * 60, false);
                target.AddBuff(BuffID.Cursed, 5 * 60, false);
            }

            //if (Main.rand.NextBool(10) && player.statLifeMax > 20) 

            //{

            //			Main.NewText("You have been cursed!");
            //	player.statLifeMax -= 20;
            //}
        }
        #endregion

        public static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EarthTrident");
            }
            if (tridentTimer >= 110)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Red, 1f);
                Main.dust[dust].noGravity = true;

                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
            int spriteWidth = NPC.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
            int spriteHeight = TextureAssets.Npc[ModContent.NPCType<OolacileKnight>()].Value.Height / Main.npcFrameCount[NPC.type];

            int spritePosDifX = (int)(NPC.frame.Width / 2);
            int spritePosDifY = NPC.frame.Height; // was npc.frame.Height - 4;

            int frame = NPC.frame.Y / spriteHeight;

            int offsetX = (int)(NPC.position.X + (NPC.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
            int offsetY = (int)(NPC.position.Y + NPC.height - Main.screenPosition.Y - spritePosDifY);

            SpriteEffects flop = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                flop = SpriteEffects.FlipHorizontally;
            }


            //Glowing Eye Effect
            for (int i = 15; i > -1; i--)
            {
                //draw 3 levels of trail
                int alphaVal = 255 - (15 * i);
                Color modifiedColour = new Color((int)(alphaVal), (int)(alphaVal), (int)(alphaVal), alphaVal);
                spriteBatch.Draw((Texture2D)TextureAssets.Gore[Mod.Find<ModGore>("Oolacile Knight Glow").Type],
                    new Rectangle((int)(offsetX - NPC.velocity.X * (i * 0.5f)), (int)(offsetY - NPC.velocity.Y * (i * 0.5f)), spriteWidth, spriteHeight),
                    new Rectangle(0, NPC.frame.Height * frame, spriteWidth, spriteHeight),
                    modifiedColour,
                    0,
                    new Vector2(0, 0),
                    flop,
                    0);
            }
        }


        #region gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Oolacile Knight Gore 1").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Oolacile Knight Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Oolacile Knight Gore 3").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Oolacile Knight Gore 4").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Oolacile Knight Gore 2").Type, 0.9f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Oolacile Knight Gore 3").Type, 0.9f);

                for (int i = 0; i < 8; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Blood Splat").Type, 0.9f);
                }
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 2));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Humanity>(), 100, 1, 1, 30));
        }
    }
}