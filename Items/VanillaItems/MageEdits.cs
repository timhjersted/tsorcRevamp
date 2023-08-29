using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.VanillaItems;

class MageEdits : GlobalItem
{

    public override void SetDefaults(Item item)
    {
        //Why is this eventide's internal name i'm literally going to go feral
        if (item.type == ItemID.SparkleGuitar)
        {
            item.mana = 25;
        }
        if (item.type == ItemID.CrimsonRod)
        {
            item.DamageType = DamageClass.MagicSummonHybrid;
        }
        if (item.type == ItemID.NimbusRod)
        {
            item.DamageType = DamageClass.MagicSummonHybrid;
        }
        if (item.type == ItemID.ClingerStaff)
        {
            item.DamageType = DamageClass.MagicSummonHybrid;
        }
        if (item.type == ItemID.NimbusRod)
        {
            item.DamageType = DamageClass.MagicSummonHybrid;
        }

        if (item.type == ItemID.FairyQueenMagicItem)
        {
            item.damage = 38;
        }
        if (item.type == ItemID.SparkleGuitar)
        {
            item.damage = 50;
        }

        //Lunar items
        if (item.type == ItemID.NebulaBlaze)
        {
            item.mana = 24;
        }
        if (item.type == ItemID.NebulaArcanum)
        {
            item.mana = 60;
        }
        if (item.type == ItemID.LastPrism)
        {
            item.mana = 30;
        }
        if (item.type == ItemID.LunarFlareBook)
        {
            item.damage = 80;
            item.mana = 39;
        }
    }
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        Player player = Main.player[Main.myPlayer];
        if (item.type == ItemID.CrimsonRod)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "DamageType", $"{(int)player.GetTotalDamage(DamageClass.MagicSummonHybrid).ApplyTo(item.damage)} magic/summon damage"));
            }
        }
        if (item.type == ItemID.NimbusRod)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "DamageType", $"{(int)player.GetTotalDamage(DamageClass.MagicSummonHybrid).ApplyTo(item.damage)} magic/summon damage"));
            }
        }
        if (item.type == ItemID.ClingerStaff)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "DamageType", $"{(int)player.GetTotalDamage(DamageClass.MagicSummonHybrid).ApplyTo(item.damage)} magic/summon damage"));
            }
        }
        if (item.type == ItemID.MagnetSphere)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "DamageType", $"{(int)player.GetTotalDamage(DamageClass.MagicSummonHybrid).ApplyTo(item.damage)} magic/summon damage"));
            }
        }
    }
}