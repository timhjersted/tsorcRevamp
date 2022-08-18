using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcVoodooShaman : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.damage = 33;
            NPC.lifeMax = 1260;
            NPC.defense = 28;
            NPC.value = 6000;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.2f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodooShamanBanner>();

            AnimationType = NPCID.Skeleton;
            Main.npcFrameCount[NPC.type] = 15;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dworc Shaman");
        }
        //yes i tweaked the drop rates. Fight Me
        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.RegenerationPotion, 1, 1, 4));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.MagicPowerPotion, 1, 1, 3));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.ManaRegenerationPotion, 1, 1, 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.IronskinPotion, 2));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.AttractionPotion>(), 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 40));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.ShockwavePotion>(), 6));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.FlaskofFire, 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.StrengthPotion>(), 16));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.CrimsonPotion>(), 4));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Defensive.BandOfCosmicPower>(), 20));

            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.SoulShekel>(), 1, 3, 5)); 
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.CharcoalPineResin>(), 2));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.Lifegem>(), 2));
        }

        //Spawns in the Jungle and in the Cavern in HM.

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            var player = spawnInfo.Player;
            bool TropicalOcean = player.position.X < 3600;
            float chance = 0f;

            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }

            if (spawnInfo.Water) return 0f;

            if (Main.hardMode && (spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneJungle) && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight && Main.dayTime) return 0.01f;
                if (spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime) return 0.035f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.025f;
                if (spawnInfo.Player.ZoneRockLayerHeight) return 0.03f;
            }
            if (Main.hardMode && TropicalOcean && spawnInfo.Player.ZoneJungle) return 0.045f;

            return chance;
        }

        float poisonStrikeTimer = 0;
        float poisonStormTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.04f, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 3f);

            bool clearLineofSight = Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1);
            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStrikeTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 18, 8, clearLineofSight, true, SoundID.Item20, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStormTimer, 480, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 25, 0, true, true, SoundID.Item100);//2,100 cursed firewall
            //tsorcRevampAIs.SimpleProjectile(NPC, ref poisonStormTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>(), 25, 0, true, true, SoundID.Item100);//2,100 cursed firewall
            //GREEN DUST  
            if (poisonStrikeTimer >= 60)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
            }
            //PINK DUST
            if (poisonStrikeTimer >= 110)
            {
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(2))
                {
                    int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 1.5f);

                    Main.dust[pink].noGravity = true;
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
            if (NPC.justHit && Main.rand.NextBool(22))
            {
                tsorcRevampAIs.Teleport(NPC, 20, true);
                poisonStrikeTimer = 10f;
            }


            if (poisonStormTimer >= 390)
            {
                UsefulFunctions.DustRing(NPC.Center, 480 - poisonStormTimer, DustID.CursedTorch, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.velocity = Vector2.Zero;
                }
            }


            if (Main.rand.NextBool(100))
            {
                poisonStrikeTimer = 110;
            }

            //DEMON SPIRIT ATTACK
            if (Main.rand.NextBool(425))
            {
                int num65 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + Main.rand.Next(-500, 500), NPC.Center.Y + Main.rand.Next(-600, 600), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), 60, 0f, Main.myPlayer);      
            }

            //Higher alpha = more invisible
            if (NPC.justHit)
            {
                NPC.alpha = 0;
            }
            if (Main.rand.NextBool(100))
            {
                NPC.alpha = 225;
                NPC.netUpdate = true;
            }
            if (Main.rand.NextBool(200))
            {
                NPC.alpha = 0; //0 is fully visible 225 is almost invisible
                NPC.netUpdate = true;
            }
            if (Main.rand.NextBool(250))
            {
                NPC.ai[3] = 1;
                NPC.life += 5;
                if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                NPC.ai[1] = 1f;
                NPC.netUpdate = true;
            }
        }

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
            }
        }
    }
}
