using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Magic;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Projectiles.Ranged.Ammo;
using tsorcRevamp.Projectiles.Ranged.Runeterra;
using tsorcRevamp.Projectiles.Throwing;

namespace tsorcRevamp.Systems.Electrocute;

public class ElectrocutePlayer : ModPlayer
{

    public const float BadRangedDmg = 10f;
    
    public List<int> ElectrocuteProjectiles = new List<int>();

    public const bool ExceptionRule = false;
    
    public static List<int> ElectrocuteExceptions = new List<int>();

    public static List<int> MolotovList = new List<int>();
    
    public static List<int> CelebrationMK2List = new List<int>();
    
    public static List<int> ToxicCatalyzerList = new List<int>();
    public static List<int> VirulentCatalyzerList = new List<int>();
    public static List<int> BiohazardList = new List<int>();

    public override void Load()
    {
        if (ExceptionRule)
        {
            ElectrocuteExceptions = new List<int>()
            {
                ProjectileID.BeeArrow, //turns into bee projectile after dying, effectively a double projectile
                ProjectileID.Beenade, //same as above
                ProjectileID.HallowStar, //Holy Arrows
                ProjectileID.CrystalShard, //Crystal Bullet
                ProjectileID.CursedDartFlame,
                ProjectileID.BlackBolt, //Onyx Blaster
                ProjectileID.SuperStarSlash, //Super Star Shooter
                ProjectileID.DD2PhoenixBowShot,
                ProjectileID.PhantasmArrow,
                ProjectileID.VortexBeaterRocket,
                ProjectileID.MoonlordArrowTrail, //Luminite Arrow
                ModContent.ProjectileType<PyroclasticFlow>(),
                ModContent.ProjectileType<Ice5Ball>(),
                ModContent.ProjectileType<Ice3Icicle>(),
                ModContent.ProjectileType<Bolt3Lightning>(),
                ProjectileID.ShadowFlame, //Shadow Fury
                ModContent.ProjectileType<Bolt1Bolt>(),
                ModContent.ProjectileType<PowerBoltExplosion>(),
                ModContent.ProjectileType<VenomBladeField>(),
                ModContent.ProjectileType<ShadowSparkle>(), //Shadowspark Bullet
                ModContent.ProjectileType<BlackArrow>(), //Gastraphetes
                ModContent.ProjectileType<AlienBlindingLaser>(),
                ModContent.ProjectileType<RadioactiveBlindingLaser>(),
                ModContent.ProjectileType<NuclearMushroom>(),
                ModContent.ProjectileType<NuclearMushroomExplosion>(),
            };
            MolotovList  = new List<int>()
            {
                ProjectileID.MolotovCocktail,
                ProjectileID.MolotovFire,
                ProjectileID.MolotovFire2,
                ProjectileID.MolotovFire3
            };
            CelebrationMK2List = new List<int>()
            {
                ProjectileID.Celeb2Rocket,
                ProjectileID.Celeb2RocketExplosive,
                ProjectileID.Celeb2RocketLarge,
                ProjectileID.Celeb2RocketExplosiveLarge
            };
            ToxicCatalyzerList = new List<int>()
            {
                ModContent.ProjectileType<ToxicCatShot>(),
                ModContent.ProjectileType<ToxicCatDetonator>(),
                ModContent.ProjectileType<ToxicCatExplosion>(),
            };
            VirulentCatalyzerList = new List<int>()
            {
                ModContent.ProjectileType<VirulentCatShot>(),
                ModContent.ProjectileType<VirulentCatDetonator>(),
                ModContent.ProjectileType<VirulentCatExplosion>(),
            };
            BiohazardList = new List<int>()
            {
                ModContent.ProjectileType<BiohazardShot>(),
                ModContent.ProjectileType<BiohazardDetonator>(),
                ModContent.ProjectileType<BiohazardExplosion>(),
            };
            
        }
    }

    public override void Unload()
    {
        ElectrocuteExceptions.Clear();
        MolotovList.Clear();
        CelebrationMK2List.Clear();
        ToxicCatalyzerList.Clear();
        VirulentCatalyzerList.Clear();
        BiohazardList.Clear();
    }

    public override void PreUpdateBuffs()
    {
        if (Electrocute.Enabled)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Player.HeldItem.DamageType == DamageClass.Ranged && !Player.HasBuff(ModContent.BuffType<ElectrocuteCooldown>()))
                {
                    Player.AddBuff(ModContent.BuffType<Electrocute>(), 3);
                }
            }
        }
    }

    public bool HasMolotov = false;
    public bool HasCeleb2 = false;
    public bool HasToxicCat = false;
    public bool HasViruCat = false;
    public bool HasBiohazard = false;
    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Player.HasBuff(ModContent.BuffType<Electrocute>()) && !Player.HasBuff(ModContent.BuffType<ElectrocuteCooldown>()) 
            && proj.DamageType == DamageClass.Ranged)
        {
            bool hasSameType = ElectrocuteProjectiles.Contains(proj.type);
        
            bool isException = ElectrocuteExceptions.Contains(proj.type);
        
            bool isMolotov = MolotovList.Contains(proj.type);
            bool isCeleb2 = CelebrationMK2List.Contains(proj.type);
            bool isToxicCat = ToxicCatalyzerList.Contains(proj.type);
            bool isViruCat = VirulentCatalyzerList.Contains(proj.type);
            bool isBiohazard = BiohazardList.Contains(proj.type);
        
            if (!hasSameType && !isException && !isMolotov && !isCeleb2 && !isToxicCat && !isViruCat && !isBiohazard)
            {
                ElectrocuteProjectiles.Add(proj.type);
            }

            if (isMolotov && !HasMolotov)
            {
                ElectrocuteProjectiles.Add(proj.type);
                HasMolotov = true;
            }
            if (isCeleb2 && !HasCeleb2)
            {
                ElectrocuteProjectiles.Add(proj.type);
                HasCeleb2 = true;
            }
            if (isToxicCat && !HasToxicCat)
            {
                ElectrocuteProjectiles.Add(proj.type);
                HasToxicCat = true;
            }
            if (isViruCat && !HasViruCat)
            {
                ElectrocuteProjectiles.Add(proj.type);
                HasViruCat = true;
            }
            if (isBiohazard && !HasBiohazard)
            {
                ElectrocuteProjectiles.Add(proj.type);
                HasBiohazard = true;
            }
            
            if (ElectrocuteProjectiles.Count >= 3)
            {
                Player.AddBuff(ModContent.BuffType<ElectrocuteCooldown>(), Electrocute.Cooldown);
                ElectrocuteProjectiles.Clear();
                HasMolotov  = false;
                HasCeleb2 = false;
                HasToxicCat  = false;
                HasViruCat  = false;
                HasBiohazard = false;
                Main.NewText("proc");
            }
        }
    }
}