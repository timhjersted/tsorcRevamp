using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories;
using tsorcRevamp.Content.Items.Accessories.Defensive;
using tsorcRevamp.Content.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Content.Items.Accessories.Other;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Projectiles.Enemy.DemonFamily;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    // Defiled Demon — late pre-HM boss near the Fire Temple, tier 2 of the demon family (shared AI in DemonBase.cs).
    // Element: black flame with curse-pink accents; every hit builds Curse (breath +10, lob/fountain +15, punisher +20,
    // contact +25 — a full curse at ~4–7 hits). Phase 2 adds the base's reversed second breath sweep.
    // Signature: Fire Fountain — eight shells thrown up off its shoulders that hang overhead, glint, and dive at the player
    // one by one, while it walks again underneath them.
    [AutoloadBossHead]
    class DefiledDemon : DemonBase
    {
        // Fire Fountain: 45t tell → burst of 4 shells at tick 46 and 4 more at 66 → 40t recovery (ends at 106; the shells
        // keep falling after it walks off). Each shell rises at -8 px/t (apex ≈ 40t, 10 tiles up), hangs, and shell i dives
        // at FountainFirstDropDelay + i·FountainDropInterval after the first burst: drops 116..200, 12t apart.
        const int FountainTellTicks = 45;
        const int FountainBurstGap = 20;
        const int FountainShellsPerBurst = 4;
        const int FountainRecoveryTicks = 40;
        const int FountainFirstDropDelay = 70; // 40t rise + 30t hang
        const int FountainDropInterval = 12;
        const float FountainLaunchSpeed = 8f;
        const float FountainSpreadSpeed = 1.2f; // vx per shell slot: -1.8 .. +1.8 px/t across the four
        const int FountainHeadroomTiles = 12;
        const float FountainMaxRange = 640f;

        int fountainDamage;

        protected override DemonTier Tier => DemonTier.Defiled;

        public override void SetDefaults()
        {
            NPC.width = 50;
            NPC.height = 120;
            NPC.damage = 55;
            NPC.defense = 15;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 76000;
            NPC.boss = true;
            NPC.rarity = 14;
            SetSharedDefaults();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.DefiledDemon.DespawnHandler"), Color.HotPink, DustID.PinkTorch);
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
        }

        protected override void InitializeStats()
        {
            breathDamage = 21;
            lobDamage = 22;
            punisherDamage = 26;
            fountainDamage = 22;
        }

        protected override void AddSignatureCards(List<DemonAttack> cards, bool isPhaseTwo)
        {
            cards.Add(DemonAttack.FireFountain);
        }

        protected override bool IsSignatureOpen(DemonAttack attack, float distance, bool lineOfSight)
        {
            if (attack == DemonAttack.FireFountain)
            {
                return distance <= FountainMaxRange && HasHeadroom(FountainHeadroomTiles);
            }

            return false;
        }

        protected override void TickSignature(Player target)
        {
            if (State == DemonAttack.FireFountain)
            {
                TickFireFountain(target);
                return;
            }

            EndAttack();
        }

        void TickFireFountain(Player target)
        {
            int firstBurstTick = FountainTellTicks + 1;
            int secondBurstTick = firstBurstTick + FountainBurstGap;

            // Tell: black smoke boils up off its shoulders with curse-pink sparks, the body flushing pink; growl at tick 1.
            if (AttackTimer <= FountainTellTicks)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.6f, Pitch = 0.2f });
                }

                float progress = AttackTimer / (float)FountainTellTicks;
                bodyFlash = progress * 0.5f;

                // Wraith (black) columns rising from a 60px-wide band across the shoulders, 1 in 3 with a pink accent.
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 spot = Horns + new Vector2(Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(0f, 20f));
                        Vector2 riseVelocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(2f, 4f) * (0.5f + progress));
                        Dust smoke = Dust.NewDustPerfect(spot, DemonElement.FlameDust(Tier), riseVelocity, 60, default, MathHelper.Lerp(1.2f, 2f, progress));
                        smoke.noGravity = true;
                    }

                    if (Main.rand.NextBool(3))
                    {
                        DemonElement.SpawnAccent(Tier, Horns - new Vector2(30f, 0f), 60, 16, new Vector2(0f, -3f), 1.3f);
                    }

                    Lighting.AddLight(Horns, DemonElement.LightColor(Tier) * (0.4f + progress));
                }

                return;
            }

            bool firstBurst = AttackTimer == firstBurstTick;
            bool secondBurst = AttackTimer == secondBurstTick;

            if (firstBurst || secondBurst)
            {
                PlaySound(SoundID.Item45 with { Volume = 0.7f, Pitch = -0.4f });
                EmitBurst(Horns, 18, 4f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int firstShell = 0;
                    if (secondBurst)
                    {
                        firstShell = FountainShellsPerBurst;
                    }

                    // Shell i's drop tick is measured from its own spawn, so the second burst's shells subtract the gap.
                    int burstOffset = AttackTimer - firstBurstTick;

                    for (int slot = 0; slot < FountainShellsPerBurst; slot++)
                    {
                        int shellIndex = firstShell + slot;
                        float spreadX = (slot - 1.5f) * FountainSpreadSpeed;
                        Vector2 velocity = new Vector2(spreadX, -FountainLaunchSpeed + Main.rand.NextFloat(-0.4f, 0.4f));
                        float dropTick = FountainFirstDropDelay + shellIndex * FountainDropInterval - burstOffset;
                        Vector2 spawn = Horns + new Vector2(spreadX * 8f, 0f);

                        Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, velocity, ModContent.ProjectileType<DemonFireball>(), fountainDamage, 0f, Main.myPlayer, (float)Tier, DemonFireball.ModeFountain, dropTick);
                    }
                }

                return;
            }

            // Between the bursts it stays armored; after the second, the recovery has none.
            if (AttackTimer < secondBurstTick)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                return;
            }

            NPC.velocity.X *= 0.9f;

            if (AttackTimer >= secondBurstTick + FountainRecoveryTicks)
            {
                EndAttack();
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<AncientDemonBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<EyeOfTheGods>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BarrierRing>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrackedDragonStone>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 1, 1, 2));
            npcLoot.Add(notExpertCondition);
        }

        protected override void SpawnDeathGores()
        {
            int[] gores = { 1, 2, 3, 2, 3 };

            for (int i = 0; i < gores.Length; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                int goreType = Mod.Find<ModGore>("Ancient Demon Gore " + gores[i]).Type;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, goreType, 1f);
            }
        }
    }
}
