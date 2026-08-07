using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// PROTOTYPE invader puppet — gear/dye pass only.
    ///
    /// The visual loadout (Wolf Mask + Plaguebringer's Cloak + Jim's Leggings, silver/brown dyes,
    /// Yoraiz0r's Scowl for the glowing scowl) is the finished part.  Weapons are now final: a
    /// vanilla Titanium Sword swung as a Greatsword, plus a vanilla Shadowflame Bow ranged phase.
    /// Still no bespoke combos or special-attack templates — the kit runs on the base slash/stab/
    /// archetype-combo loop.  Everything else (rendering, dye slots, accessory slots, movement,
    /// telegraphs) comes free from <see cref="PuppetNPC"/>.
    /// </summary>
    [AutoloadBossHead]
    public class Blaidd : PuppetNPC
    {
        // PLACEHOLDER sprite (copy of StuddedLeatherWarrior_Head_Boss.png) — replace with bespoke art.
        // The body itself is fully puppet-rendered off the armor items, so no Blaidd.png is needed
        // (PuppetNPC.Texture already points at NPCs/Puppets/PuppetPlaceholder).
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/Blaidd_Head_Boss";

        protected override string InvaderTitle => "Blaidd";

        // ── Loadout (all vanilla) ─────────────────────────────────────────────────
        // Wolf Mask (1841), Plaguebringer's Cloak (5046, bodySlot 235), Jim's Leggings (1565).
        protected override int HeadArmorItemType => ItemID.WolfMask;
        protected override int BodyArmorItemType => ItemID.PlaguebringerChestplate; // display name: "Plaguebringer's Cloak"
        protected override int LegsArmorItemType => ItemID.JimsLeggings;

        protected override int HeadArmorDyeItemType => ItemID.BrightBrownDye;
        protected override int BodyArmorDyeItemType => ItemID.SilverDye;
        protected override int LegsArmorDyeItemType => ItemID.SilverDye;

        // Yoraiz0r's Scowl — the glowing-eye vanity accessory (ItemID.Yoraiz0rDarkness, 3581).
        // Left undyed per the reference sheet.
        protected override int[] AccessoryItemTypes => new int[]
        {
            ItemID.Yoraiz0rDarkness,
        };
        protected override int[] AccessoryDyeItemTypes => new int[]
        {
            0,
        };

        // ── Weapons (both vanilla) ────────────────────────────────────────────────
        // Titanium Sword (1199) — useStyle Swing, damage 61, useTime 20.  Under
        // WeaponArchetypeTables.DetectMelee that auto-detects as Broadsword (the Greatsword rule
        // needs damage >= 70 AND useTime >= 28), so the archetype is force-overridden below.
        protected override int MeleeWeaponItemType => ItemID.TitaniumSword;
        // Shadowflame Bow (3052, ItemID.ShadowFlameBow) — useStyle Shoot + useAmmo Arrow, so
        // DetectRanged returns WeaponArchetype.Bow naturally; no ranged override needed.
        protected override int RangedWeaponItemType => ItemID.ShadowFlameBow;

        /// <summary>Blaidd swings the Titanium Sword as a two-handed greatsword.  The base's
        /// auto-detection would call it a Broadsword (see MeleeWeaponItemType note), so this uses
        /// the documented per-puppet escape hatch on <see cref="PuppetNPC.MeleeArchetype"/> to
        /// force the heavier combo pool.</summary>
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;

        protected override int MeleeDamage => 70;
        // ~0.7x melee, matching the ratios on the other mixed-kit puppets
        // (AbyssalNinjaInvader 80/55, ShadowNinja 48/34).
        protected override int RangedDamage => 50;

        // ── Ranged tuning (bow: long reach, deliberate draw) ──────────────────────
        protected override float RangedRange => 560f;
        protected override float MinRangedRange => 220f;
        protected override int RangedTelegraphTicks => 48; // bow draw
        protected override int RangedRecoveryTicks => 40;
        protected override int RangedCooldownAfterUse => 240;
        protected override Color RangedTelegraphFlashColor => new Color(170, 90, 220);

        // ── Combat tuning (minimal, mid-tier SHM band) ────────────────────────────
        protected override float TopSpeed => 2.8f;
        protected override float Acceleration => 0.10f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 170f;
        protected override bool CanStab => true;
        protected override bool SlowDownBeforeMelee => false;

        protected override int MeleeTelegraphTicks => 38;
        protected override int MeleeRecoveryTicks => 24;
        protected override int StabTelegraphTicks => 40;
        protected override int StabAttackTicks => 8;
        protected override int StabRecoveryTicks => 34;

        protected override Color MeleeTelegraphFlashColor => new Color(215, 225, 255);

        protected override int TeleportTelegraphTicks => 130;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(185, 185, 195);

        // Prototype is a melee pursuer: recover, then immediately resume the chase.
        protected override int CasualStrollChance => 0;

        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6f;

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                attackRange: 640f);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            // Mid-tier SHM band for reference: DarkKnight 3000 hp / 30 def,
            // CrystalKnight 2800 / 50, DarkBloodKnight 3200 / 67.  Blaidd sits mid-pack.
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 3000;
            NPC.defense = 35;
            NPC.damage = 0; // all damage comes from the weapon hitbox
            NPC.knockBackResist = 0.18f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 12000f;
            NPC.boss = true; // matches every sibling puppet: HP bar + no distance despawn while prototyping
            NPC.npcSlots = 5f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 38f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
        }

        // Placeholder drop table — no bespoke rewards until the kit is designed.
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 400, 600));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<Blaidd>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Items.StaminaDroplet>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.55f);
        }

        /// <summary>
        /// Shadowflame Bow shot.  Puppet ranged attacks don't fire the backing vanilla item's real
        /// projectile — the held bow only drives archetype detection and the draw animation — so this
        /// spawns the mod's hostile <see cref="Projectiles.Enemy.Weapons.EnemyShadowflameArrow"/>
        /// (same pattern as ShadowNinja's EnemyTaintedArrow bow shot).
        /// </summary>
        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.22f);
            Vector2 aimAt = target.Center + target.velocity * 10f;
            Vector2 toTarget = aimAt - muzzle;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            SoundEngine.PlaySound(SoundID.Item102 with { Volume = 0.8f, PitchVariance = 0.12f }, NPC.Center);

            float spread = MathHelper.ToRadians(Main.rand.NextFloat(-3.5f, 3.5f));
            Vector2 velocity = toTarget.RotatedBy(spread) * 12f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                muzzle,
                velocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyShadowflameArrow>(),
                RangedDamage,
                3f,
                Main.myPlayer);
        }
    }
}
