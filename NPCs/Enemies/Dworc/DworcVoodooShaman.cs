using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    public class DworcVoodooShaman : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
        }
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.damage = 0;
            NPC.lifeMax = 750;
            NPC.defense = 28;
            NPC.value = 7500;
            NPC.width = 18;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.2f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodooShamanBanner>();
            AnimationType = NPCID.Skeleton;
            // "Poison Strike" — fast aimed poison bolt
            UsefulFunctions.AddAttack(NPC, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 18, 8, SoundID.Item20, 0);
            // "Poison Storm" — heavy lobbed ball that bursts into a poison storm (no LOS needed). The shrinking-ring
            // telegraph runs 90t (see AI()), so telegraphTime: 90 makes the poise telegraph/commit window match the
            // visible ring; commitFraction: 0.5f = first half of the shrink is cancellable, second half committed.
            UsefulFunctions.AddAttack(NPC, 480, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 25, 0, SoundID.Item20, needsLineOfSight: false, weight: 0.2f, telegraphTime: 90, commitFraction: 0.5f);
            // "Demon Spirit" — summons a homing spirit (rare; no LOS needed)
            UsefulFunctions.AddAttack(NPC, 200, ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), 20, 0, SoundID.Item20, needsLineOfSight: false, weight: 0.05f);

            // Step 6 caster lever: remember last-known position before patrolling.
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RemembersLastKnownPos = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().NavSearchRadius = 50; // Phase 2: SmartFighter4AI movement
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanUseRopes = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanGoInvisible = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().InvisibleAlpha = 205;
            EvasiveProfile.EvasiveCloak(NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), cloakChance: 0.30f, threatRange: 220);
            // Poise (a stagger guarantees a cloak reveal) + knockback flinch are tuned centrally in
            // tsorcRevampGlobalNPC.PopulatePoiseProfiles() (GlobalNPC.cs) — not here.
            EvasiveProfile.DworcShaman(NPC.GetGlobalNPC<tsorcRevampGlobalNPC>());
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanSelfHeal = true;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SelfHealAmount = 20;
        }
        //yes i tweaked the drop rates. Fight Me
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 31, 1, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 31, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 31, 1, 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 32));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 35));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 26));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofNanites, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 26));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 24));        
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 3, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 3, 3, 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 3));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 8));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofLight, 2, 1, 2));
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
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.04f, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 3f, canPounce: false);

            // (Removed the old justHit cast-timer reset + 1/22 hit-teleport — superseded by the universal poise
            //  stagger windup-cancel and the EvasiveOnHit reactions, incl. the evasive teleport in the DworcShaman profile.)

            //Big poison storm telegraph
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool telegraphingPoisonStorm = globalNPC.CurrentAttack.type == ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>();
            float stormTelegraphStart = globalNPC.CurrentAttack.timerCap - 90f;
            if (telegraphingPoisonStorm && globalNPC.ProjectileTimer >= stormTelegraphStart)
            {
                float telegraphRadius = MathHelper.Clamp(globalNPC.CurrentAttack.timerCap - globalNPC.ProjectileTimer, 0f, 90f);
                UsefulFunctions.DustRing(NPC.Center, telegraphRadius, DustID.CursedTorch, 12, 4);

                Lighting.AddLight(NPC.Center, Color.Orange.ToVector3() * 5);
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.velocity *= 0.3f;
                }
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
