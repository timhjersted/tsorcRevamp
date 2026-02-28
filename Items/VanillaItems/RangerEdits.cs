using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class RangerEdits : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.Minishark)
            {
                item.damage = 5;
            }
            if (item.type == ItemID.CrystalBullet)
            {
                item.damage = 1;
            }
            if (item.type == ItemID.ChlorophyteBullet)
            {
                //chlorophyte bullets are fucking stupid, dont @ me
                item.damage = 1; //from 10
            }
            if (item.type == ItemID.CrystalDart)
            {
                //this still makes them incredibly good in pre-hm and gives hardmode darts a chance to be viable, as crystal darts would just outclass them in our closed spaces
                item.damage = 1; //from 14
            }

            if (item.type == ItemID.BloodRainBow) 	
            {
                //given out as reward for beating a red knight so I think this is fair, also hard to use
                item.damage = 12; //from 14
            }

            if (item.type == ItemID.HolyArrow)
            {
                item.damage = 5;
            }

            if (item.type == ItemID.BeesKnees)
            {
                item.damage = 19;
            }

            if (item.type == ItemID.Beenade)
            {
                item.damage = 10;
            }

            if (item.type == ItemID.CoinGun)
            {
                item.damage = 25; 
                item.useTime = 10;
                item.useAnimation = 10;
            }

            if (item.type == ItemID.DaedalusStormbow)
            {
                //item.damage = 45;
                //item.mana = 20;
            }


            if (item.type == ItemID.PhoenixBlaster)
            {
                item.damage = 25; 
            }

            //Why is this eventide's internal name i'm literally going to go feral
            if (item.type == ItemID.FairyQueenRangedItem)
            {
                item.damage = 42; //vanilla 50
            }

            //Golem items
            if (item.type == ItemID.Stynger)
            {
                item.damage = 55; //vanilla 45
            }

            if (item.type == ItemID.StakeLauncher)
            {
                item.damage = 120; //SHM
            }

            if (item.type == ItemID.ElfMelter)
            {
                item.damage = 110; //SHM
            }

            if (item.type == ItemID.CandyCornRifle)
            {
                item.damage = 110; //SHM
            }

            if (item.type == ItemID.JackOLanternLauncher)
            {
                item.damage = 115; //SHM
            }

            if (item.type == ItemID.ChainGun)
            {
                item.damage = 40; //SHM
                item.useTime = 3;
            }

            if (item.type == ItemID.SnowmanCannon)
            {
                item.damage = 125; //SHM
            }

            //Lunar items
            if (item.type == ItemID.Phantasm)
            {
                item.damage = 42; 
            }

            //Vanilla damage is 53
            if (item.type == ItemID.Tsunami)
            {
                item.damage = 46;
            }

            if (item.type == ItemID.DD2BetsyBow)
            {
                item.damage = 36;
            }

            if (item.type == ItemID.Xenopopper)
            {
                item.damage = 70; //SHM
            }

            if (item.type == ItemID.ElectrosphereLauncher)
            {
                item.damage = 80; //SHM
            }
        }
        
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (tsorcRevampWorld.NewSlain != null)
            {
                if (item.type == ItemID.DD2BetsyBow & tsorcRevampWorld.isHellkiteDragonDead)
                {
                    damage *= 1.25f;
                }
                if (item.type == ItemID.ElectrosphereLauncher & NPC.downedMartians)
                {
                    damage *= 1.15f;
                }
                if (item.type == ItemID.Xenopopper & NPC.downedMartians)
                {
                    damage *= 1.15f;
                }
            }
        }
    }
}