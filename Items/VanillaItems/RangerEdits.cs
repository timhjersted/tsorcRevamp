using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems;

class RangerEdits : GlobalItem
{
    public override void SetDefaults(Item item)
    {
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
        }

        if(item.type == ItemID.HolyArrow)
        {
            item.damage = 1;
        }

        //Why is this eventide's internal name i'm literally going to go feral
        if (item.type == ItemID.FairyQueenRangedItem)
        {
            item.damage = 30;
        }

        //Lunar items
        if (item.type == ItemID.Phantasm)
        {
            item.damage = 35;
        }
    }
}