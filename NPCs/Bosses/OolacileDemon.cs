using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Magic.Bands;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Projectiles.Enemy.DemonFamily;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    // Oolacile Demon — early pre-HM boss, tier 1 of the demon family (shared AI in DemonBase.cs).
    // Element: orange torch fire, On Fire. Signature: Spirit Ring — the Desert Spirit's ghost-skull sorcery (DemonSpiritSkull),
    // green-dripping and +50 Poison buildup per hit. Its punisher also throws one homing skull instead of a fireball.
    [AutoloadBossHead]
    class OolacileDemon : DemonBase
    {
        // Spirit Ring: 50t violet converge onto the body → 8 skulls outward at 5 px/t (45° gaps ≈ 125px at 10 tiles) →
        // phase 2 only, a second ring rotated 22.5° 40t later → 40t recovery.
        const int RingTellTicks = 50;
        const int RingSecondTick = 90;
        const int RingRecoveryTicks = 40;
        const int RingSkulls = 8;
        const float RingSpeed = 5f;
        const float RingMaxRange = 640f;
        const float RingSecondRotation = MathHelper.Pi / 8f; // 22.5°
        const float PunisherSkullSpeed = 8f;
        const float PunisherSkullGravity = 1.06f;

        int ringDamage;

        // Server-only: the first ring's rotation, so the second can sit in its gaps. Only the server spawns, so no sync.
        float ringRotation;

        protected override DemonTier Tier => DemonTier.Oolacile;

        public override void SetDefaults()
        {
            NPC.width = 50;
            NPC.height = 120;
            NPC.damage = 46;
            NPC.defense = 8;
            NPC.lifeMax = 4800;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 60000;
            NPC.boss = true;
            NPC.rarity = 7;
            SetSharedDefaults();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.OolacileDemon.DespawnHandler"), Color.OrangeRed, DustID.Torch);
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
        }

        // After the Eye of Cthulhu: tougher, worth more, and every attack hits a little harder.
        protected override void InitializeStats()
        {
            breathDamage = 13;
            lobDamage = 13;
            punisherDamage = 14;
            ringDamage = 17;

            if (NPC.downedBoss1)
            {
                NPC.defense = 18;
                NPC.value = 90000;
                breathDamage = 15;
                lobDamage = 15;
                punisherDamage = 15;
                ringDamage = 19;
            }
        }

        protected override void AddSignatureCards(List<DemonAttack> cards, bool isPhaseTwo)
        {
            cards.Add(DemonAttack.SpiritRing);

            if (isPhaseTwo)
            {
                cards.Add(DemonAttack.SpiritRing);
            }
        }

        protected override bool IsSignatureOpen(DemonAttack attack, float distance, bool lineOfSight)
        {
            if (attack == DemonAttack.SpiritRing)
            {
                return lineOfSight && distance <= RingMaxRange;
            }

            return false;
        }

        protected override void TickSignature(Player target)
        {
            if (State == DemonAttack.SpiritRing)
            {
                TickSpiritRing(target);
                return;
            }

            EndAttack();
        }

        void TickSpiritRing(Player target)
        {
            int lastRingTick = RingTellTicks;
            if (phaseTwo)
            {
                lastRingTick = RingSecondTick;
            }

            // Tell: violet sorcery spirals in onto the body, which flashes violet.
            if (AttackTimer <= RingTellTicks)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                FaceTowards(target.Center.X);

                if (AttackTimer == 1)
                {
                    PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = -0.2f });
                }

                float progress = AttackTimer / (float)RingTellTicks;

                // Shadowflame (27, violet) and GreenTorch poison motes (matching the skulls) drawn in from 90px to 10px around the body.
                if (!Main.dedServ)
                {
                    float radius = MathHelper.Lerp(90f, 10f, progress);

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius * 1.3f);
                        int dustType = DustID.Shadowflame;
                        if (i == 2)
                        {
                            dustType = DustID.GreenTorch;
                        }

                        Dust mote = Dust.NewDustPerfect(NPC.Center + offset, dustType, -offset * 0.07f, 40, default, MathHelper.Lerp(0.9f, 1.6f, progress));
                        mote.noGravity = true;
                    }

                    Lighting.AddLight(NPC.Center, 0.4f * progress, 0.1f * progress, 0.5f * progress);
                }

                bodyFlash = progress * 0.5f;
                bodyFlashTint = Color.MediumPurple;
            }

            bool firstRing = AttackTimer == RingTellTicks;
            bool secondRing = phaseTwo && AttackTimer == RingSecondTick;

            if (firstRing || secondRing)
            {
                PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.5f });

                // Release: a violet ring of motes bursting outward with the skulls.
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                        Dust burst = Dust.NewDustPerfect(NPC.Center, DustID.Shadowflame, burstVelocity, 40, default, 1.8f);
                        burst.noGravity = true;
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (firstRing)
                    {
                        ringRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                    }
                    else
                    {
                        ringRotation += RingSecondRotation;
                    }

                    FireSpiritRing();
                }

                return;
            }

            // Between the two rings it stays armored; after the last one, the recovery has none.
            if (AttackTimer < lastRingTick)
            {
                Poise.AttackCommitted = true;
                NPC.velocity.X *= 0.85f;
                return;
            }

            NPC.velocity.X *= 0.9f;

            if (AttackTimer >= lastRingTick + RingRecoveryTicks)
            {
                EndAttack();
            }
        }

        // Server-only. ai[0] = the demon's target, which the skulls home on.
        void FireSpiritRing()
        {
            float angleStep = MathHelper.TwoPi / RingSkulls;

            for (int i = 0; i < RingSkulls; i++)
            {
                float angle = ringRotation + i * angleStep;
                Vector2 velocity = angle.ToRotationVector2() * RingSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity, ModContent.ProjectileType<DemonSpiritSkull>(), ringDamage, 0f, Main.myPlayer, NPC.target);
            }
        }

        // The punisher is a ghost-skull too, lobbed high from the horns (the legacy "player running away" shot).
        protected override void FirePunisher(Player target)
        {
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(Horns, target.Center, PunisherSkullSpeed, PunisherSkullGravity, true, true);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), Horns, velocity, ModContent.ProjectileType<DemonSpiritSkull>(), punisherDamage, 7f, Main.myPlayer, NPC.target);
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<OolacileDemonBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BandOfCosmicPower>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PermanentShinePotion>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PermanentNightOwlPotion>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.CloudinaBalloon));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 1, 3));
            npcLoot.Add(notExpertCondition);
        }

        protected override void SpawnDeathGores()
        {
            int[] gores = { 1, 2, 3, 2, 3 };

            for (int i = 0; i < gores.Length; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                int goreType = Mod.Find<ModGore>("Ancient Oolacile Demon Gore " + gores[i]).Type;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, goreType, 1f);
            }
        }
    }
}
