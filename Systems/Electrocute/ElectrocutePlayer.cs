using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Electrocute;

public class ElectrocutePlayer : ModPlayer
{

    public const float BadRangedDmg = 10f;
    
    public List<int> ElectrocuteProjectiles = new List<int>();

    public const bool ExceptionRule = true;
    
    public static List<int> ElectrocuteExceptions = new List<int>();

    public static List<int> MolotovList = new List<int>();
    
    public static List<int> CelebrationMK2List = new List<int>();

    public override void Load()
    {
        if (ExceptionRule)
        {
            ElectrocuteExceptions = new List<int>()
            {
                ProjectileID.BeeArrow, //turns into bee projectile after dying, effectively a double projectile
                ProjectileID.Beenade, //same as above
                ProjectileID.HallowStar, //holy arrows
                ProjectileID.CrystalShard, //crystal bullet
                ProjectileID.CursedDartFlame,
                ProjectileID.BlackBolt, //Onyx Blaster
                ProjectileID.SuperStarSlash, //super star shooter
                ProjectileID.DD2PhoenixBowShot,
                ProjectileID.PhantasmArrow,
                ProjectileID.VortexBeaterRocket,
                ProjectileID.MoonlordArrowTrail, //luminite arrow
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
        }
    }

    public override void Unload()
    {
        ElectrocuteExceptions.Clear();
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
    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Player.HasBuff(ModContent.BuffType<Electrocute>()) && !Player.HasBuff(ModContent.BuffType<ElectrocuteCooldown>()) 
            && proj.DamageType == DamageClass.Ranged)
        {
            bool hasSameType = ElectrocuteProjectiles.Contains(proj.type);
        
            bool isException = ElectrocuteExceptions.Contains(proj.type);
        
            bool isMolotov = MolotovList.Contains(proj.type);
            bool isCeleb2 = CelebrationMK2List.Contains(proj.type);
        
            if (!hasSameType && !isException && !isMolotov && !isCeleb2)
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
            
            if (ElectrocuteProjectiles.Count >= 3)
            {
                Player.AddBuff(ModContent.BuffType<ElectrocuteCooldown>(), Electrocute.Cooldown);
                ElectrocuteProjectiles.Clear();
                HasMolotov  = false;
                HasCeleb2 = false;
                Main.NewText("proc");
            }
        }
    }
}