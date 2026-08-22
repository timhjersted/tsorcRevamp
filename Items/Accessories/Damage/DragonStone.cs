using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class DragonStone : ModItem
    {
        public const float Potency = 5f;
        public const float PotencyDivisor = 2f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Potency, PotencyDivisor);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            var modPlayer  = player.GetModPlayer<DragonStonePlayer>();
            modPlayer.DragonStoneImmunity = true;
            modPlayer.DragonStonePotency = true;
        }
    }

    public class DragonStonePlayer : ModPlayer
    {
        public bool DragonStoneImmunity;
        public bool DragonStonePotency;
        public override void ResetEffects()
        {
            DragonStoneImmunity = false;
            DragonStonePotency = false;
        }

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            
                if (DragonStoneImmunity && damageSource.SourcePlayerIndex > -1)
                {
                    int NT = Main.npc[damageSource.SourceNPCIndex].type;
                    if (NT == NPCID.DemonEye
                        || NT == NPCID.DemonEye2
                        || NT == NPCID.EaterofSouls
                        || NT == NPCID.CursedSkull
                        || NT == NPCID.Hornet
                        || NT == NPCID.Harpy
                        || NT == NPCID.CaveBat
                        || NT == NPCID.JungleBat
                        || NT == NPCID.Hellbat
                        || NT == NPCID.Vulture
                        || NT == NPCID.Demon
                        || NT == NPCID.VoodooDemon
                        || NT == NPCID.Pixie
                        || NT == NPCID.WyvernHead || NT == NPCID.WyvernLegs || NT == NPCID.WyvernBody || NT == NPCID.WyvernBody2 || NT == NPCID.WyvernBody3 || NT == NPCID.WyvernTail
                        || NT == NPCID.GiantBat
                        || NT == NPCID.Corruptor || NT == NPCID.VileSpit
                        || NT == NPCID.Gastropod
                        || NT == NPCID.WanderingEye
                        || NT == NPCID.IlluminantBat
                        || NT == NPCID.Probe
                        || NT == NPCID.IceBat
                        || NT == NPCID.Lavabat
                        || NT == NPCID.GiantFlyingFox
                        || NT == NPCID.RedDevil
                        || NT == NPCID.VampireBat
                        || NT == NPCID.IceElemental
                        || NT == NPCID.PigronCorruption
                        || NT == NPCID.PigronHallow
                        || NT == NPCID.PigronCrimson
                        || NT == NPCID.Crimera
                        || NT == NPCID.MossHornet
                        || NT == NPCID.CrimsonAxe
                        || NT == NPCID.FloatyGross
                        || NT == NPCID.Moth
                        || NT == NPCID.Bee
                        || NT == NPCID.FlyingFish
                        || NT == NPCID.FlyingSnake
                        || NT == NPCID.AngryNimbus
                        || NT == NPCID.Parrot
                        || NT == NPCID.Reaper
                        || NT == NPCID.IchorSticker
                        || NT == NPCID.DungeonSpirit
                        || NT == NPCID.Ghost
                        || NT == NPCID.ElfCopter
                        || NT == NPCID.Flocko
                        || NT == NPCID.MartianDrone
                        || NT == NPCID.MartianProbe
                        || NT == NPCID.ShadowFlameApparition
                        || NT == NPCID.MothronSpawn
                        || NT == NPCID.GraniteFlyer
                        || NT == NPCID.FlyingAntlion
                        || NT == NPCID.DesertDjinn
                        || NT == NPCID.WyvernHead
                        || NT == NPCID.Harpy
                        || NT == NPCID.CultistDragonHead
                        || NT == NPCID.SandElemental
                        || NT == NPCID.SporeBat
                        || NT == ModContent.NPCType<CloudBat>())
                    {
                        return true;
                    }
                }
                return base.ImmuneTo(damageSource, cooldownCounter, dodgeable);
        }
    }

    public class DragonStoneNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public Player LastHitDragonStonePlayer;
        public bool DragonStoneActive;
        public bool OiledFirstApplication;
        public override void ResetEffects(NPC npc)
        {
            if (LastHitDragonStonePlayer != null)
            {
                var dragonPlayer = LastHitDragonStonePlayer.GetModPlayer<DragonStonePlayer>();
                DragonStoneActive = dragonPlayer.DragonStonePotency;
            }
            else
            {
                DragonStoneActive = false;
            }

            OiledFirstApplication = true;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff(BuffID.BetsysCurse) && DragonStoneActive)
            {
                modifiers.Defense.Flat -= 40 * DragonStone.Potency - 40;
            }
            if (npc.HasBuff(BuffID.Ichor) && DragonStoneActive)
            {
                modifiers.Defense.Flat -= 15 * DragonStone.Potency - 15;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            var dragonPlayer = player.GetModPlayer<DragonStonePlayer>();
            if (dragonPlayer.DragonStonePotency)
            {
                LastHitDragonStonePlayer = player;
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            var dragonPlayer = player.GetModPlayer<DragonStonePlayer>();
            if (dragonPlayer.DragonStonePotency)
            {
                LastHitDragonStonePlayer = player;
            }
        }
        /// <summary>
        /// Calculates Dragon Stone DoT damage bonus. Modded debuffs also use this in tsorcRevampGlobalNPC.
        /// Also adds stacking Oiled debuff damage bonus.
        /// </summary>
        /// <param name="npcType"></param>
        /// <param name="dotBase"></param>
        /// <param name="needsNerf"></param>
        /// <param name="isFire"></param>
        /// <returns></returns>
        public static float AddDragonStonePotencyDoT(in NPC npcType, in float dotBase, bool needsNerf = false, bool isFire = true)
        {
            var dragonNpc = npcType.GetGlobalNPC<DragonStoneNPC>();
            bool oiledFirstApplication = dragonNpc.OiledFirstApplication; //use this so entire function stays in same mode
            float oiledDoTBonus = 0f;
            if (npcType.lifeRegen > 0)
            {
                npcType.lifeRegen = 0;
            }
            if (npcType.HasBuff(BuffID.Oiled) && isFire)
            {
                if (oiledFirstApplication)
                {
                    dragonNpc.OiledFirstApplication = false; //set this to false so next calculation will recognize it
                }
                else
                {
                    oiledDoTBonus += 25f;
                }
            }
            if (!dragonNpc.DragonStoneActive)
            {
                return oiledDoTBonus;
            }
            float finalDoTPerS = 0f;
            finalDoTPerS += (dotBase * DragonStone.Potency) - dotBase;
            if (needsNerf)
            {
                finalDoTPerS /= DragonStone.PotencyDivisor;
            }
            if (npcType.HasBuff(BuffID.Oiled) && isFire)
            {
                finalDoTPerS += 25f * DragonStone.Potency - 25f;
            }
            return oiledDoTBonus + finalDoTPerS;
        }

        public static int StackingDebuffDoT(in NPC npc, in int projType, in int dotBase)
        {
            int stacks = 0;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.type == projType && proj.ai[0] == 1f && proj.ai[1] == (float)npc.whoAmI)
                {
                    stacks++;
                }
            }
            return stacks * dotBase;
        }

        public static void CustomUpdateLifeRegen(NPC npc, ref int damage)
        {
            var dragonNpc = npc.GetGlobalNPC<DragonStoneNPC>();
            float DotPerS = 0;
            if (npc.onFire)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 4);
            }
            if (npc.onFire3)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 15);
            }
            if (npc.onFire2)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 24);
            }
            if (npc.onFrostBurn)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 8);
            }
            if (npc.onFrostBurn2)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 25);
            }
            if (npc.shadowFlame)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 15);
            }
            if (npc.poisoned)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 6, isFire:false);
            }
            if (npc.venom)
            {
                DotPerS += AddDragonStonePotencyDoT(npc, 30, isFire:false);
            }
            if (npc.javelined)
            {
                int dotBase = StackingDebuffDoT(npc, ProjectileID.BoneJavelin,3);
                DotPerS += AddDragonStonePotencyDoT(npc, dotBase, isFire:false);
            }
            if (npc.tentacleSpiked)
            {
                int dotBase = StackingDebuffDoT(npc,  ProjectileID.TentacleSpike, 3);
                DotPerS += AddDragonStonePotencyDoT(npc, dotBase, isFire:false);
            }
            if (npc.HasBuff(BuffID.BloodButcherer))
            {
                int dotBase = StackingDebuffDoT(npc, ProjectileID.BloodButcherer, 4);
                DotPerS += AddDragonStonePotencyDoT(npc, dotBase, isFire:false);
            }
            if (npc.daybreak)
            {
                int dotBase = StackingDebuffDoT(npc, ProjectileID.Daybreak, 100);
                DotPerS += AddDragonStonePotencyDoT(npc, dotBase, true);
            }

            npc.lifeRegen -= (int)(DotPerS * 2f);
            damage += (int)DotPerS;
            dragonNpc.OiledFirstApplication = false;
        }
    }
}
