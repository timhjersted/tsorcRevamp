using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcVoodoomaster : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath29;
            NPC.npcSlots = 3;
            NPC.damage = 0;
            NPC.lifeMax = 250;
            NPC.defense = 7;
            NPC.value = 1250; // life / 2 
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.3f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodoomasterBanner>();

            AnimationType = NPCID.Skeleton;

            if (Main.hardMode)
            {
                NPC.lifeMax = 500;
                NPC.defense = 14;
                NPC.value = 2500;
                NPC.damage = 42;
                NPC.knockBackResist = 0.1f;
            }

            UsefulFunctions.AddAttack(NPC, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 7, 8, SoundID.Item20);
            UsefulFunctions.AddAttack(NPC, 700, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 9, 0, SoundID.Item100);
        }
        //excuse me while i drop Every Potion Known To Mankind holy hell
        //these dudes oughtta be called alchemists or something
        //actually you know what? i have the ability to change that
        //goodbye dworc voodoomaster, hello dworc alchemist
        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            Player player = Main.player[NPC.target];

            npcLoot.Add(ItemDropRule.Common(ItemID.SpellTome, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.SpelunkerPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 22));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 10, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.HealingPotion, 35, 3, 5));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 33, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 22));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 32));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofFire, 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 25));
            //npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.BossItems.CursedSkull>(), 50));
            //npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Defensive.BandOfCosmicPower>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 3, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 4));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 5, 5, 9));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 5, 1));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 14));
        }


        //Spawns in the Jungle Underground and in the Cavern.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
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

            float chance = 0f;

            if (spawnInfo.Water) return 0f;

            if ((spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneJungle) && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson)
            {
                if (spawnInfo.Player.ZoneOverworldHeight) return 0.004f;
                if (spawnInfo.Player.ZoneDirtLayerHeight) return 0.008f;
                if (spawnInfo.Player.ZoneRockLayerHeight && Main.dayTime) return 0.095f;
                if (spawnInfo.Player.ZoneRockLayerHeight && !Main.dayTime) return 0.035f;
            }
            if (Main.hardMode && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneBeach && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson) return 0.005f;

            return chance;
        }

        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 0.8f, 0.02f, 0.2f, true, enragePercent: 0.5f, enrageTopSpeed: 1.6f, canPounce: false);

            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 520)//SHRINKING CIRCLE DUST
            {
                UsefulFunctions.DustRing(NPC.Center, 700 - NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer, DustID.CursedTorch, 12, 4);
                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.velocity = Vector2.Zero;
                }                
            }

            //IF HIT BEFORE PINK DUST TELEGRAPH, RESET TIMER, BUT CHANCE TO BREAK STUN LOCK
            //(WORKS WITH 2 TELEGRAPH DUSTS, AT 60 AND 110)
            if (NPC.justHit && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer < NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTelegraphStart)
            {
                if (Main.rand.NextBool(3))
                {
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 110;
                    NPC.netUpdate = true;
                }
                else
                {
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            if (NPC.justHit && Main.rand.NextBool(24))
            {
                tsorcRevampAIs.QueueTeleport(NPC, 20, true, 50);
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 70f;
                NPC.netUpdate = true;
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
                NPC.alpha = 220;
            }
            if (Main.rand.NextBool(250))
            {
                NPC.life += 5;
                if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                NPC.netUpdate = true;
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
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