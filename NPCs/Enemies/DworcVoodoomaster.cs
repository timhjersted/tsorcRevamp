using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcVoodoomaster : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath29;
            NPC.damage = 20;
            NPC.lifeMax = 212;
            NPC.defense = 7;
            NPC.value = 4200;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.3f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodoomasterBanner>();

            AnimationType = NPCID.Skeleton;
            Main.npcFrameCount[NPC.type] = 15;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dworc Voodoo Master");
        }
        public override void OnKill()
        {
            Player player = Main.player[NPC.target];

            if (Main.rand.NextBool(50)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.Defensive.BandOfCosmicPower>());
            if (Main.rand.NextBool(50)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BossItems.CursedSkull>());
            //if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Armors.TibalMask>()); TO-DO
            if (Main.rand.NextBool(50)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
            if (Main.rand.NextBool(20)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.FlaskofFire);
            if (Main.rand.NextBool(12)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
            if (Main.rand.NextBool(25)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
            if (Main.rand.NextBool(12)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
            if (Main.rand.NextBool(3)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, Main.rand.Next(1, 3));

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.rand.NextBool(5))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.HealingPotion, Main.rand.Next(3, 5));
            }

            if (Main.rand.NextBool(25)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GillsPotion);
            if (Main.rand.NextBool(25)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.HunterPotion);
            if (Main.rand.NextBool(2)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion, Main.rand.Next(1, 3));
            if (Main.rand.NextBool(12)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ShinePotion);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SpelunkerPotion);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SwiftnessPotion);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.WaterWalkingPotion);
            if (Main.rand.NextBool(2)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.BattlePotion);
            if (Main.rand.NextBool(10)) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SpellTome);
            if (Main.rand.NextBool(10))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Magic.RedClothHat>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Magic.RedClothTunic>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Armors.Magic.RedClothPants>());
            }
        }

        float poisonStrikeTimer = 0;
        float poisonStormTimer = 0;

        //Spawns in the Jungle Underground and in the Cavern.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if ((spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneJungle) && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight) return 0.005f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.01f;
                if (spawnInfo.Player.ZoneRockLayerHeight && Main.dayTime) return 0.0143f;
                if (spawnInfo.Player.ZoneRockLayerHeight && !Main.dayTime) return 0.033f;
            }
            if (Main.hardMode && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneBeach && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) return 0.0005f;

            return chance;
        }

        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1f, 0.02f, 0.2f, true, enragePercent: 0.3f, enrageTopSpeed: 2);

            bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStrikeTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 7, 8, clearLineofSight, true, SoundID.Item20, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStormTimer, 300, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 9, 0, true, true, SoundID.Item100);

            if (poisonStrikeTimer >= 60)//GREEN DUST
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
            }

            if (poisonStrikeTimer >= 110)//PINK DUST
            {
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.5f);

                    Main.dust[pink].noGravity = true;
                }
            }
            if (poisonStormTimer >= 120)//SHRINKING CIRCLE DUST
            {
                UsefulFunctions.DustRing(NPC.Center, 300 - poisonStormTimer, DustID.CursedTorch, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.velocity = Vector2.Zero;
                }
            }

            //IF HIT BEFORE PINK DUST TELEGRAPH, RESET TIMER, BUT CHANCE TO BREAK STUN LOCK
            //(WORKS WITH 2 TELEGRAPH DUSTS, AT 60 AND 110)
            if (NPC.justHit && poisonStrikeTimer <= 109)
            {
                if (Main.rand.NextBool(3))
                {
                    poisonStrikeTimer = 110;
                }
                else
                {
                    poisonStrikeTimer = 0;
                }
            }
            if (NPC.justHit && Main.rand.NextBool(18))
            {
                tsorcRevampAIs.Teleport(NPC, 20, true);
                poisonStrikeTimer = 70f;
            }

            //Transparency. Higher alpha = more invisible
            if (NPC.justHit)
            {
                NPC.alpha = 0;
            }
            if (Main.rand.NextBool(200))
            {
                NPC.alpha = 0;
            }
            if (Main.rand.NextBool(50))
            {
                NPC.alpha = 210;
            }
            if (Main.rand.NextBool(250))
            {
                NPC.life += 5;
                if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                NPC.netUpdate = true;
            }
        }

        #region Gore
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                }
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion, 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.Lifegem>(), 1);
            }
        }
        #endregion
    }
}