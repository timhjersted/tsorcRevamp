using Terraria;
using Terraria.ID;
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
                item.damage = 20; //from 14
                item.mana = 10;
            }

            if (item.type == ItemID.HolyArrow)
            {
                item.damage = 1;
            }

            if (item.type == ItemID.DaedalusStormbow)
            {
                item.damage = 45;
                item.mana = 20;
            }

            //Why is this eventide's internal name i'm literally going to go feral
            if (item.type == ItemID.FairyQueenRangedItem)
            {
                item.damage = 30;
            }

            //Golem items
            if (item.type == ItemID.Stynger)
            {
                item.damage = 55; //vanilla 45
            }

            //Lunar items
            if (item.type == ItemID.Phantasm)
            {
                item.mana = 30;
            }

            //same damage as Gastraphetes, vanilla damage is 53
            if (item.type == ItemID.Tsunami)
            {
                item.damage = 35;
            }
        }
        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (item.ammo != AmmoID.None && player.GetModPlayer<tsorcRevampPlayer>().AmmoBox)
            {
                crit += item.damage;
            }
        }
    }
}