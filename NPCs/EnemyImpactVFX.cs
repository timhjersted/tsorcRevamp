using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.Basilisk;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Enemies.GhostFighter;
using tsorcRevamp.NPCs.Enemies.JungleWyvernJuvenile;
using tsorcRevamp.NPCs.Enemies.ParasyticWorm;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// The material an enemy sheds when struck. This is visual flavour only; never use it for a
    /// mechanical tell because blood dust is replaced when the player's gore setting is off.
    /// </summary>
    internal enum EnemyImpactMaterial
    {
        None,
        Blood,
        GhostDust,
        StoneBlood
    }

    /// <summary>
    /// Shared, data-driven hit and death dust for mod enemies. This catalogue is deliberately
    /// independent of tsorcRevamp's Human/Undead/etc. gameplay lists: those lists drive unrelated
    /// spawning and AI behaviour, so adding an entry here must never change how an NPC plays.
    /// </summary>
    internal static class EnemyImpactVFX
    {
        const int StandardHitDust = 3;
        const int StandardDeathDust = 16;
        const int HitDustCooldownTicks = 3;

        // Every non-puppet hostile ModNPC that needs a default impact material belongs here. This
        // gives future additions one obvious, type-safe place to declare their body material.
        static readonly Dictionary<int, EnemyImpactMaterial> ImpactProfiles = new()
        {
            // Humanoids, grounded undead, and living creatures.
            [ModContent.NPCType<AbandonedStump>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<AbyssLurker>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<AncientDemonOfTheAbyss>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Archdeacon>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Assassin>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<BasiliskHunter>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<BasiliskShifter>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<BasiliskWalker>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<BlackKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Byakhee>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<CloudBat>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<CorruptedHornet>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<CosmicCrystalLizard>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<CrystalKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DarkBloodKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DarkKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DemonLordApocalypse>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DemonWheel>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DiscipleOfAttraidies>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DungeonMage>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Dunlending>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DworcAbysswalker>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DworcAlchemist>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DworcFleshhunter>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DworcVenomsniper>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<DworcVoodooShaman>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<EvilEye>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<FallenNecromancer>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<FirebombHollow>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<FirebombHollowOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<FireLurker>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Gigas>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<GreatBlackKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<GuardianCorruptor>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HollowSoldier>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HollowSoldierOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HollowSpearman>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HollowSpearmanOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HollowWarrior>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HollowWarriorOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Hydra>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<HydrisNecromancer>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<IceGigas>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<IceSkeleton>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleSentree>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleWyvernJuvenileHead>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleWyvernJuvenileBody>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleWyvernJuvenileBody2>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleWyvernJuvenileBody3>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleWyvernJuvenileLegs>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<JungleWyvernJuvenileTail>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<KnightOfGwyn>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LivingShroomThief>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LothricBlackKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LothricBlackKnightOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LothricKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LothricKnightOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LothricSpearKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<LothricSpearKnightOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ManHunter>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ManOfWar>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Massacre>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MarilithSeeker>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MinotaurMage>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MindflayerKingServant>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MountedSandsprog>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MountedSandsprogMage>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MindflayerServant>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MushroomCreature>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<MutantToad>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Necromancer>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ObsidianJellyfish>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<OolacileDemon>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<OolacileKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<OolacileSorcerer>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Parasprite>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ParasyticWormHead>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ParasyticWormBody>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ParasyticWormTail>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Plaguesmith>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<QuaraClutchCrab>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<QuaraHydromancer>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<QuaraMantassin>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<QuaraPincher>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<RedCloudHunter>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ResentfulSeedling>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<RedKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<RedKnightTest>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<RingedKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<RingedKnightOriginal>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Sahagin>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Sandsprog>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SandsprogMage>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<ShadowMage>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SerpentOfTheAbyssHead>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SerpentOfTheAbyssBody>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SerpentOfTheAbyssTail>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SlograII>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SnowOwl>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<SpellboundGhoul>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<TaurusKnight>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Tetsujin>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<TibianAmazon>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<TibianValkyrie>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<TibianValkyrieSmart4>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Tonberry>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<UndeadCaster>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<VampireBat>()] = EnemyImpactMaterial.Blood,
            [ModContent.NPCType<Warlock>()] = EnemyImpactMaterial.Blood,

            // Incorporeal enemies: visual ghost dust, never red blood.
            [ModContent.NPCType<ArmoredWraith>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<AttraidiesIllusion>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<AttraidiesManifestation>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<BarrowWight>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<BarrowWightNemesis>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<BarrowWightPhantom>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<CorruptedElemental>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<DemonElemental>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<DemonSpirit>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<CrazedDemonSpirit>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfAHollowWarrior>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfAHollowWarriorOriginal>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfTheDarkmoonKnight>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfTheDrowned>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfTheDrownedOriginal>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfTheForgottenKnight>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<GhostOfTheForgottenWarrior>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<HumanityPhantom>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<HydrisElemental>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<MarilithSpiritTwin>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<MindflayerIllusion>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<WaterSpirit>()] = EnemyImpactMaterial.GhostDust,
            [ModContent.NPCType<Willowisp>()] = EnemyImpactMaterial.GhostDust,

            // Mineral life gets a dense grey, gravity-affected stone spray instead of blood.
            [ModContent.NPCType<StoneGolem>()] = EnemyImpactMaterial.StoneBlood,

            // Technical NPCs, projectiles implemented as NPCs, and Eland's bespoke blood effects.
            [ModContent.NPCType<DestroyerLaserProbe>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<Eland>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<FrozenGigasStatue>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<GaibonFireball>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<KhaiosTransitionOrb>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<PinwheelFireball>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<PrimeLaserProbe>()] = EnemyImpactMaterial.None,
            [ModContent.NPCType<ViciousSpit>()] = EnemyImpactMaterial.None,
        };

        internal static void Spawn(NPC npc, NPC.HitInfo hit, tsorcRevampGlobalNPC globalNPC)
        {
            if (!TryGetMaterial(npc, out EnemyImpactMaterial material))
                return;

            bool died = npc.life <= 0;
            // A segmented creature can throw blood when the player hits a body piece, but only its
            // independent head gets the death burst. That keeps the juvenile wyvern readable
            // without multiplying one death into a shower from every linked segment.
            if (died && npc.realLife >= 0)
                return;
            int tick = (int)Main.GameUpdateCount;
            if (!died)
            {
                // Very rapid multihit weapons otherwise consume the dust budget before their
                // individual splats can read. This caps one NPC at one small spray every 3 ticks.
                if (tick - globalNPC.LastEnemyImpactVFXTick < HitDustCooldownTicks)
                    return;

                globalNPC.LastEnemyImpactVFXTick = tick;
            }

            int count = died ? GetDeathDustCount(npc, material) : GetHitDustCount(material, hit.Crit);
            int direction = hit.HitDirection == 0 ? -npc.direction : hit.HitDirection;
            Vector2 sprayDirection = new Vector2(direction, -0.42f).SafeNormalize(Vector2.UnitY);

            for (int i = 0; i < count; i++)
            {
                Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.36f, npc.height * 0.30f);
                Vector2 velocity = died
                    ? Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 5.8f) + npc.velocity * 0.16f
                    : sprayDirection.RotatedByRandom(0.72f) * Main.rand.NextFloat(0.85f, 3.2f) + npc.velocity * 0.10f;

                SpawnMaterialDust(material, position, velocity, died);
            }
        }

        static bool TryGetMaterial(NPC npc, out EnemyImpactMaterial material)
        {
            material = default;

            if (npc.friendly)
                return false;

            if (ImpactProfiles.TryGetValue(npc.type, out material))
            {
                return material != EnemyImpactMaterial.None;
            }

            // A PuppetNPC is always an embodied humanoid. This covers every present and future
            // derived puppet without needing to remember a second registration entry.
            if (npc.ModNPC is PuppetNPC)
            {
                material = EnemyImpactMaterial.Blood;
                return true;
            }

            return false;
        }

        static int GetHitDustCount(EnemyImpactMaterial material, bool crit)
        {
            int count = material == EnemyImpactMaterial.GhostDust ? StandardHitDust + 1 : StandardHitDust;
            return crit ? count + 2 : count;
        }

        static int GetDeathDustCount(NPC npc, EnemyImpactMaterial material)
        {
            float sizeMultiplier = MathHelper.Clamp((npc.width + npc.height) / 80f, 1f, 1.6f);
            int count = (int)(StandardDeathDust * sizeMultiplier);
            return material == EnemyImpactMaterial.GhostDust ? count + 5 : count;
        }

        static void SpawnMaterialDust(EnemyImpactMaterial material, Vector2 position, Vector2 velocity, bool died)
        {
            int dustType = DustID.Blood;
            Color color = default;
            bool noGravity = false;
            bool noLight = false;
            float scale = died ? Main.rand.NextFloat(0.65f, 1.10f) : Main.rand.NextFloat(0.48f, 0.88f);

            switch (material)
            {
                case EnemyImpactMaterial.GhostDust:
                    dustType = DustID.Shadowflame;
                    color = new Color(88, 48, 132);
                    noGravity = true;
                    noLight = true;
                    scale *= 0.9f;
                    velocity *= 0.72f;
                    break;

                case EnemyImpactMaterial.StoneBlood:
                    dustType = DustID.Stone;
                    color = new Color(132, 132, 142);
                    scale *= 0.9f;
                    break;
            }

            Dust dust = Dust.NewDustPerfect(position, dustType, velocity, died ? 55 : 80, color, scale);
            dust.noGravity = noGravity;
            dust.noLight = noLight;

        }
    }
}
