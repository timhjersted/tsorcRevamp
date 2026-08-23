using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
    public const float TimeWindowInSec = 2.5f;
    public bool CanElectrocute;

    public int[] ElectrocuteProjectileType1 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileType2 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileType3 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileDamage1 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileDamage2 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileDamage3 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileCritChance1 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileCritChance2 = new int[Main.maxNPCs];
    public int[] ElectrocuteProjectileCritChance3 = new int[Main.maxNPCs];
    
    public bool[] HasMolotov = new bool[Main.maxNPCs];
    public bool[] HasCeleb2 = new bool[Main.maxNPCs];
    public bool[] HasToxicCat = new bool[Main.maxNPCs];
    public bool[] HasViruCat = new bool[Main.maxNPCs];
    public bool[] HasBiohazard = new bool[Main.maxNPCs];

    public const bool ExceptionRule = false;
    
    public static List<int> ElectrocuteExceptions = new List<int>();

    public static List<int> MolotovList = new List<int>();
    
    public static List<int> CelebrationMK2List = new List<int>();
    
    public static List<int> ToxicCatalyzerList = new List<int>();
    public static List<int> VirulentCatalyzerList = new List<int>();
    public static List<int> BiohazardList = new List<int>();

    public override void ResetEffects()
    {
        CanElectrocute = false;
    }

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
                CanElectrocute  = true;
            }
        }
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Player.HasBuff(ModContent.BuffType<Electrocute>()) && !Player.HasBuff(ModContent.BuffType<ElectrocuteCooldown>()) 
            && proj.DamageType == DamageClass.Ranged)
        {
            int tT = target.whoAmI;
            int electrocuteType = ModContent.ProjectileType<ElectrocuteProjectile>();
            
            bool hasSameType = ElectrocuteProjectileType1[tT] == proj.type || ElectrocuteProjectileType2[tT] == proj.type || ElectrocuteProjectileType3[tT] == proj.type || proj.type == electrocuteType;
        
            bool isException = ElectrocuteExceptions.Contains(proj.type);
        
            bool isMolotov = MolotovList.Contains(proj.type);
            bool isCeleb2 = CelebrationMK2List.Contains(proj.type);
            bool isToxicCat = ToxicCatalyzerList.Contains(proj.type);
            bool isViruCat = VirulentCatalyzerList.Contains(proj.type);
            bool isBiohazard = BiohazardList.Contains(proj.type);
        
            if (!hasSameType && !isException && !isMolotov && !isCeleb2 && !isToxicCat && !isViruCat && !isBiohazard)
            {
                AddElectrocuteProjectile(target, proj, hit);
            }

            if (isMolotov && !HasMolotov[tT])
            {
                AddElectrocuteProjectile(target, proj, hit);
                HasMolotov[tT] = true;
            }
            if (isCeleb2 && !HasCeleb2[tT])
            {
                AddElectrocuteProjectile(target, proj, hit);
                HasCeleb2[tT] = true;
            }
            if (isToxicCat && !HasToxicCat[tT])
            {
                AddElectrocuteProjectile(target, proj, hit);
                HasToxicCat[tT] = true;
            }
            if (isViruCat && !HasViruCat[tT])
            {
                AddElectrocuteProjectile(target, proj, hit);
                HasViruCat[tT] = true;
            }
            if (isBiohazard && !HasBiohazard[tT])
            {
                AddElectrocuteProjectile(target, proj, hit);
                HasBiohazard[tT] = true;
            }
            
            ResetTimer(target, proj);
            
            if (CheckForElectrocuteProc(target))
            {
                CalculateDamageAndCritChance(target, out int damage, out int critChance);
                Projectile.NewProjectile(Projectile.GetSource_None(), target.Top, Vector2.Zero,
                    electrocuteType, damage, 0, proj.owner, critChance);
                Player.AddBuff(ModContent.BuffType<ElectrocuteCooldown>(), Electrocute.Cooldown * 60);
                ResetElectrocuteFields(target);
            }
        }
    }

    public void AddElectrocuteProjectile(NPC npc, Projectile proj, NPC.HitInfo hit)
    {
        var globalNpc = npc.GetGlobalNPC<ElectrocuteNpc>();
        var nT = npc.whoAmI;
        var pT = Player.whoAmI;
        if (ElectrocuteProjectileType1[nT] == 0)
        {
            ElectrocuteProjectileType1[nT] = proj.type;
            ElectrocuteProjectileDamage1[nT] = hit.SourceDamage;
            ElectrocuteProjectileCritChance1[nT] = proj.CritChance;
            globalNpc.ElectrocuteTimer1[pT] = 1;
        }
        else if (ElectrocuteProjectileType2[nT] == 0)
        {
            ElectrocuteProjectileType2[nT] = proj.type;
            ElectrocuteProjectileDamage2[nT] = hit.SourceDamage;
            ElectrocuteProjectileCritChance2[nT] = proj.CritChance;
            globalNpc.ElectrocuteTimer2[pT] = 1;
        }
        else if (ElectrocuteProjectileType3[nT] == 0)
        {
            ElectrocuteProjectileType3[nT] = proj.type;
            ElectrocuteProjectileDamage3[nT] = hit.SourceDamage;
            ElectrocuteProjectileCritChance3[nT] = proj.CritChance;
            globalNpc.ElectrocuteTimer3[pT] = 1;
        }
    }

    public void ResetTimer(NPC npc, Projectile proj)
    {
        var globalNpc = npc.GetGlobalNPC<ElectrocuteNpc>();
        var pT = Player.whoAmI;
        if (proj.type == ElectrocuteProjectileType1[npc.whoAmI])
        {
            globalNpc.ElectrocuteTimer1[pT] = 1;
        }
        if (proj.type == ElectrocuteProjectileType2[npc.whoAmI])
        {
            globalNpc.ElectrocuteTimer2[pT] = 1;
        }
        if (proj.type == ElectrocuteProjectileType3[npc.whoAmI])
        {
            globalNpc.ElectrocuteTimer3[pT] = 1;
        }
    }

    public bool CheckForElectrocuteProc(NPC npc)
    {
        if (ElectrocuteProjectileType1[npc.whoAmI] != 0 && ElectrocuteProjectileType2[npc.whoAmI] != 0 && ElectrocuteProjectileType3[npc.whoAmI] != 0)
        {
            return true;
        }
        return false;
    }

    public void CalculateDamageAndCritChance(NPC npc, out int damage, out int critChance)
    {
        int baseDamage = ElectrocuteProjectileDamage1[npc.whoAmI] + ElectrocuteProjectileDamage2[npc.whoAmI]  + ElectrocuteProjectileDamage3[npc.whoAmI];
        critChance = 0;
        damage = (int)Player.GetTotalDamage(DamageClass.Ranged).ApplyTo(baseDamage);
        List<int> critChances = new List<int>()
        {
            ElectrocuteProjectileCritChance1[npc.whoAmI],
            ElectrocuteProjectileCritChance2[npc.whoAmI],
            ElectrocuteProjectileCritChance3[npc.whoAmI]
        };
        foreach (int listedCritChance in critChances)
        {
            if (listedCritChance >= critChance)
            {
                critChance = listedCritChance;
            }
        }
    }

    public void ResetElectrocuteFields(NPC npc)
    {
        var globalNpc = npc.GetGlobalNPC<ElectrocuteNpc>();
        var nT = npc.whoAmI;
        var pT = Player.whoAmI;
        ElectrocuteProjectileType1[npc.whoAmI] = 0;
        ElectrocuteProjectileType2[npc.whoAmI] = 0;
        ElectrocuteProjectileType3[npc.whoAmI] = 0;
        ElectrocuteProjectileDamage1[npc.whoAmI] = 0;
        ElectrocuteProjectileDamage2[npc.whoAmI] = 0;
        ElectrocuteProjectileDamage3[npc.whoAmI] = 0;
        ElectrocuteProjectileCritChance1[npc.whoAmI] = 0;
        ElectrocuteProjectileCritChance2[npc.whoAmI] = 0;
        ElectrocuteProjectileCritChance3[npc.whoAmI] = 0;
        globalNpc.ElectrocuteTimer1[pT] = 0;
        globalNpc.ElectrocuteTimer2[pT] = 0;
        globalNpc.ElectrocuteTimer3[pT] = 0;
        HasMolotov[nT] = false;
        HasCeleb2[nT] = false;
        HasToxicCat[nT] = false;
        HasViruCat[nT] = false;
        HasBiohazard[nT] = false;
    }
}