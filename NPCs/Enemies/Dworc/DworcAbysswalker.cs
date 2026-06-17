using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    class DworcAbysswalker : ModNPC
    {
        int poisonBallDamage = 27;
        int stormBallDamage = 30;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 21;
            NPC.knockBackResist = 0;
            NPC.aiStyle = 3;
            NPC.damage = 0;
            NPC.defense = 72;
            NPC.height = 40;
            NPC.lifeMax = 3500;
            NPC.scale = 1.2f;
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.value = 10000; // life / 2.5 : was 1550 
            NPC.width = 18;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AbysswalkerBanner>();
            // "Abyss Poison Strike" — fast aimed poison bolt (green telegraph)
            UsefulFunctions.AddAttack(NPC, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), poisonBallDamage, 9, SoundID.Item20, telegraphColor: Color.GreenYellow);
            // "Abyss Storm" — heavy lobbed storm ball (blue telegraph)  [magic-ring candidate: set commitFraction: 0.5f if this is the shrinking ring]
            UsefulFunctions.AddAttack(NPC, 400, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormBall>(), stormBallDamage, 0, SoundID.Item100, weight: 0.3f, telegraphColor: Color.Blue);
            // "Demon Spirit" — summons a homing spirit (purple telegraph)
            UsefulFunctions.AddAttack(NPC, 300, ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), 35, 0, SoundID.Item100, weight: 0.2f, telegraphColor: Color.Purple);

            // Step 6 caster lever: remember last-known position before patrolling.
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RemembersLastKnownPos = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().NavSearchRadius = 50; // Phase 2: SmartFighter4AI movement
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanUseRopes = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanGoInvisible = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().InvisibleAlpha = 210;
            EvasiveProfile.EvasiveCloak(NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), cloakChance: 0.45f, threatRange: 220);
            // Poise (a stagger guarantees a cloak reveal) + knockback flinch are tuned centrally in
            // tsorcRevampGlobalNPC.PopulatePoiseProfiles() (GlobalNPC.cs) — not here.
            EvasiveProfile.DworcShaman(NPC.GetGlobalNPC<tsorcRevampGlobalNPC>());
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanSelfHeal = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SelfHealAmount = 60;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanHealAllies = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().HealAlliesChance = 400;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().HealAlliesRange = 50;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().HealAlliesPercent = 12;
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            poisonBallDamage = (int)(poisonBallDamage * tsorcRevampWorld.SHMScale);
            stormBallDamage = (int)(stormBallDamage * tsorcRevampWorld.SHMScale);
        }

        //Spawns in the Jungle Underground and in the Cavern.
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            // these are all the regular stuff you get , now lets see......
            float chance = 0;

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

            if ((player.ZoneMeteor || player.ZoneJungle) && tsorcRevampWorld.SuperHardMode && !player.ZoneDungeon && !(player.ZoneCorrupt || player.ZoneCrimson))
            {
                chance = 0.1f; // reduced from .15
            }
            if (player.ZoneDirtLayerHeight)
            {
                chance *= 1.3f;
            }
            if (player.ZoneRockLayerHeight)
            {
                chance *= 1.5f;
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            tsorcRevampAIs.FighterAI(NPC, 2f, 0.05f, 0.2f, true, enragePercent: 0.3f, enrageTopSpeed: 3, canPounce: false);

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<EarthFiendLich>())))
            {
                globalNPC.AttackList[1].timerCap = 250;
            }

            //Start the dust ring 90 frames before executing the poison storm attack
            if (globalNPC.AttackIndex == 1 && globalNPC.ProjectileTimer >= globalNPC.ProjectileTimerCap - 90)
            {
                UsefulFunctions.DustRing(NPC.Center, (globalNPC.AttackList[1].timerCap + 5) - globalNPC.ProjectileTimer, DustID.BlueCrystalShard, 12, 4);//the edge of this attack seems very hard to read; tried to make it more defined but no luck
                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
            }


        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 5));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 4));
        }

    }
}