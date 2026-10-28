using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Red Knight: the RedKnightPuppet body with the Red attack pool (TryStartRed), its stats, spawns and loot.
    /// </summary>
    public class RedKnight : RedKnightPuppet, IHumanoidMeleeHitEffects
    {
        #region Defaults
        NPCDespawnHandler despawnHandler;

        protected override string InvaderTitle => "Red Knight";

        public override void SetStaticDefaults()
        {
            // The puppet body is drawn by the base; Main.npcFrameCount 1 matches the placeholder bestiary sprite.
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailCacheLength[Type] = 4;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            // A humanoid puppet deals no body-contact damage; every hit is a spear, spell or bomb.
            NPC.damage = 0;
            NPC.defense = 41;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 20000; // life / 1.25 in HM
            NPC.knockBackResist = 0.3f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.12 of full knockback per ordinary hit
            NPC.lavaImmune = true;
            NPC.rarity = 2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.RedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);

            if (!Main.hardMode)
            {
                NPC.defense = 14;
                NPC.value = 12500;
                redKnightsGreatDamage = 9;
                redKnightsSpearDamage = 12;
                redMagicDamage = 7;
                redKnightsBombDamage = 15;
                NPC.boss = true;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 4000;
                NPC.defense = 75;
                NPC.value = 16000; // life / 2.5
                redKnightsGreatDamage = 38;
                redKnightsSpearDamage = 32;
                redMagicDamage = 26;
                redKnightsBombDamage = 40;
            }
            tsorcRevampGlobalNPC redKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            redKnightGlobalNPC.Agility = 0.3f;
            redKnightGlobalNPC.DirectionalDodgeRolls = true;
            redKnightGlobalNPC.PreserveJumpFacingUntilLanding = true;

            // Poise: needs sustained pressure to stagger (poise damage = weapon knockback). Tunable lever.
            redKnightGlobalNPC.PoiseMax = 40f;
            redKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            redKnightGlobalNPC.NavSearchRadius = 80;
            redKnightGlobalNPC.NavGiveUpTicks = 200;
            redKnightGlobalNPC.RemembersLastKnownPos = true;
            // This authored ranged/melee hybrid kites direct melee pressure, while distant projectile hits
            // temporarily collapse the band and make it pursue. KiteRangeMax is the complete movement opt-in.
            redKnightGlobalNPC.KiteRangeMin = 8f;
            redKnightGlobalNPC.KiteRangeMax = 15f;
            redKnightGlobalNPC.KiteLooseness = 0.28f;

            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = TeleportVisualStyle.Fire;

            // Start from the knight-family retreat profile, then remove its random on-hit blink below.
            EvasiveProfile.RedKnight(redKnightGlobalNPC);
            redKnightGlobalNPC.EvasiveRetreatJump = false;
            redKnightGlobalNPC.EvasiveRetreatDash = false;
            redKnightGlobalNPC.EvasiveDodgeRoll = true;
            // Teleport is now an authored sub-50% attack, never a random hit reaction. The remaining
            // retreat reactions are deliberately infrequent so multihit weapons cannot make RK spasm.
            redKnightGlobalNPC.EvasiveTeleportAway = false;
            redKnightGlobalNPC.EvasiveOnHitChanceDenominator = 6;
            redKnightGlobalNPC.EvasiveOnHitCooldownTicks = 120;
        }
        #endregion

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            if (Main.hardMode && P.ZoneDungeon && !(P.ZoneCorrupt || P.ZoneCrimson) && Main.rand.NextBool(1200)) return 1;

            if (Main.hardMode && P.ZoneMeteor && !(P.ZoneCorrupt || P.ZoneCrimson) && P.ZoneRockLayerHeight && Main.rand.NextBool(1250)) return 1;

            if (Main.hardMode && P.ZoneUnderworldHeight && Main.rand.NextBool(1100)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.NextBool(500)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneUnderworldHeight && Main.rand.NextBool(300)) return 1;

            return 0;
        }
        #endregion

        #region AI
        public override void AI()
        {
            if (!Main.hardMode)
            {
                despawnHandler.TargetAndDespawn(NPC.whoAmI);
            }

            base.AI();
        }

        private protected override bool TryStartControllerAttack(Player target, tsorcRevampGlobalNPC globalNPC)
        {
            return specialAttacks.TryStartRed(NPC, target, globalNPC);
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        #region Loot
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {

            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.RegenerationPotion, 30));
            npcLoot.Add(hmCondition);
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 3);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 20);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop);
            shmCondition.OnSuccess(drop2);
            npcLoot.Add(shmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<StaminaVessel>()));
        }
        #endregion

        #region Debuffs
        // PuppetMeleeHitbox calls this on the hit player's machine after Hurt.
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(BuffID.OnFire, 3 * 60, false);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<Crippled>(), 3 * 60, false); // loss of flight mobility
        }
        #endregion
    }
}
