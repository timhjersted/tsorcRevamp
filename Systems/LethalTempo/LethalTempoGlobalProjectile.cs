using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Melee;
using tsorcRevamp.Projectiles.Melee.Axes;
using tsorcRevamp.Projectiles.Melee.Boomerangs;
using tsorcRevamp.Projectiles.Melee.Broadswords;
using tsorcRevamp.Projectiles.Melee.Flails;
using tsorcRevamp.Projectiles.Melee.Hammers;
using tsorcRevamp.Projectiles.Melee.Shortswords;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Systems.LethalTempo;

public class LethalTempoGlobalProjectile : GlobalProjectile
{
    public bool AppliedLethalTempo = false;
    public override bool InstancePerEntity => true;
    public List<int> Banned = new List<int>()
    {
        ProjectileID.WoodYoyo,
        ProjectileID.Rally,
        ProjectileID.CorruptYoyo,
        ProjectileID.CrimsonYoyo,
        ProjectileID.JungleYoyo,
        ProjectileID.Code1,
        ProjectileID.HiveFive,
        ProjectileID.Valor,
        ProjectileID.Cascade,
        ProjectileID.FormatC,
        ProjectileID.Gradient,
        ProjectileID.Chik,
        ProjectileID.HelFire,
        ProjectileID.Amarok,
        ProjectileID.Code2,
        ProjectileID.Yelets,
        ProjectileID.RedsYoyo,
        ProjectileID.ValkyrieYoyo,
        ProjectileID.Kraken,
        ProjectileID.TheEyeOfCthulhu,
        ProjectileID.Terrarian,
        ProjectileID.WoodenBoomerang,
        ProjectileID.EnchantedBoomerang,
        ProjectileID.Shroomerang,
        ProjectileID.FruitcakeChakram,
        ProjectileID.BloodyMachete,
        ProjectileID.IceBoomerang,
        ProjectileID.Trimarang,
        ProjectileID.ThornChakram,
        ProjectileID.Flamarang,
        ProjectileID.CombatWrench,
        ProjectileID.Bananarang,
        ProjectileID.BouncingShield,
        ProjectileID.LightDisc,
        ProjectileID.PossessedHatchet,
        ProjectileID.ChainGuillotine,
        ProjectileID.Anchor,
        ProjectileID.DripplerFlailExtraBall,
        ProjectileID.FlowerPowPetal,
        ProjectileID.FlaironBubble,
        ProjectileID.FlyingKnife,
        ProjectileID.ShadowFlameKnife,
        ProjectileID.EatersBite, //Scourge of the Corruptor
        ProjectileID.TinyEater, //  ^^^
        ProjectileID.VampireKnife,
        ProjectileID.Daybreak, 
        ProjectileID.FinalFractal, //Zenith
        ProjectileID.IceBolt, //Ice Blade
        ProjectileID.Starfury,
        ProjectileID.EnchantedBeam, //Enchanted Sword
        ProjectileID.BladeOfGrass,
        ProjectileID.IceSickle,
        ProjectileID.FrostBoltSword,
        ProjectileID.SwordBeam,
        ProjectileID.Waffle,
        ProjectileID.TrueNightsEdge,
        ProjectileID.DeathSickle,
        ProjectileID.SeedlerNut,
        ProjectileID.SeedlerThorn,
        ProjectileID.OrnamentFriendly, //Christmas Tree Sword
        ProjectileID.OrnamentStar, //     ^^^^
        ProjectileID.PaladinsHammerFriendly,
        ProjectileID.TerraBlade2Shot,
        ProjectileID.InfluxWaver,
        ProjectileID.DD2SquireSonicBoom, //Flying Dragon
        ProjectileID.StarWrath,
        ProjectileID.Meowmere,
        
        ModContent.ProjectileType<ThrowingAxe>(),
        ModContent.ProjectileType<Caltrop>(),
        ModContent.ProjectileType<ShatteredMoonlightProjectile>(),
        ModContent.ProjectileType<YellowTailFishbone>(),
        ModContent.ProjectileType<BarbarousThornBladeBriar>(),
        ModContent.ProjectileType<ThornDecapitatorThorn>(),
        ModContent.ProjectileType<CMSCrescent>(),
        ModContent.ProjectileType<CrescentTrue>(),
        ModContent.ProjectileType<PilgrimArcaneBall>(),
        ModContent.ProjectileType<StarstormProjectile>(),
        ModContent.ProjectileType<AbyssalStarProj>(),
        ModContent.ProjectileType<YianBlaze>(),
        ModContent.ProjectileType<YianBlaze2>(),
        ModContent.ProjectileType<BerserkerNightmareAura>(),
        ModContent.ProjectileType<HeavensTearAura>(),
        ModContent.ProjectileType<SunderingLightAura>(),
        ModContent.ProjectileType<EphemeralThrowingAxeProj>(),
        ModContent.ProjectileType<EphemeralThrowingAxeProj2>(),
        ModContent.ProjectileType<ForgottenRisingSunProj>(),
        ModContent.ProjectileType<Limit>(),
        ModContent.ProjectileType<LonginusHeld>(),
        ModContent.ProjectileType<LonginusThrown>(),
        81, //idk why but Longinus uses id 81 somehow
        ModContent.ProjectileType<Bolt1Revamped>(), //Doomhammer
        ModContent.ProjectileType<DoomhammerFireball>(), //^^^^^^^^^^
        ModContent.ProjectileType<AncientFireAxeFireballBurst>() //^^
        
    };
    public List<int> Exceptions = new List<int>()
    {
    };

    public static List<int> Flails = new List<int>()
    {
        ProjectileID.Mace,
        ProjectileID.FlamingMace,
        ProjectileID.BallOHurt,
        ProjectileID.TheMeatball,
        ProjectileID.BlueMoon,
        ProjectileID.Sunfury,
        ModContent.ProjectileType<MoonfuryBall>(),
        ProjectileID.TheDaoofPow,
        ProjectileID.DripplerFlail,
        ProjectileID.FlowerPow,
        ProjectileID.Flairon,
        ModContent.ProjectileType<HeavensTearBall>(),
        ModContent.ProjectileType<BerserkerNightmareBall>(),
        ModContent.ProjectileType<SunderingLightBall>()
    };
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player player = Main.player[projectile.owner];
        var modPlayer = player.GetModPlayer<LethalTempoPlayer>();
        bool flailCondition = Flails.Contains(projectile.type) && projectile.ai[0] == 0;
        bool bannedCondition = Banned.Contains(projectile.type);
        if (projectile.DamageType == DamageClass.Melee && player.HasBuff(ModContent.BuffType<LethalTempo>()) && !AppliedLethalTempo && !bannedCondition)
        {
            if (modPlayer.Stacks < LethalTempoPlayer.MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.002f }, player.Center);
            }
            else if (modPlayer.Stacks == LethalTempoPlayer.MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.003f }, player.Center);
            }
            player.AddBuff(ModContent.BuffType<LethalTempo>(), LethalTempoPlayer.Duration * 60);
            if (!Exceptions.Contains(projectile.type) && !flailCondition)
            {
                AppliedLethalTempo = true;
            }
        }
    }
}