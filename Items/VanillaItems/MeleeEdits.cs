using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;

namespace tsorcRevamp.Items.VanillaItems
{
    class MeleeEdits : GlobalItem
    {
        static readonly int ManaBase1 = 20;
        static readonly int ManaBase2 = 10;
        static readonly int ManaBase3 = 25;
        static readonly int ManaDelay = 1200;
        public override void SetDefaults(Item item)
        {
            //Lunar items
            if (item.type == ItemID.DayBreak)
            {
                item.mana = 44;
            }
            if (item.type == ItemID.Terrarian)
            {
                item.mana = 20;
            }
            if(item.type == ItemID.PiercingStarlight)
            {
                item.damage = 50;
            }
            if (item.type == ItemID.NightsEdge)
            {
                item.mana = 10;
            }
            if (item.type == ItemID.TrueNightsEdge)
            {
                item.damage = 85;
                item.mana = 15;
            }
            if (item.type == ItemID.Excalibur)
            {
                item.mana = 8;
            }
            if (item.type == ItemID.TrueExcalibur)
            {
                item.damage = 75;
                item.mana = 12;
            }
            if (item.type == ItemID.TerraBlade)
            {
                item.mana = 20;
            }
            if (item.type == ItemID.TheHorsemansBlade)
            {
                item.damage = 200;
                item.useTime = 20;
                item.useAnimation = 20;
                item.mana = 12;
            }
        }
        public override bool CanShoot(Item item, Player player)
        {
            int manaCost1 = (int)(ManaBase1 * player.manaCost);
            int manaCost2 = (int)(ManaBase2 * player.manaCost);
            int manaCost3 = (int)(ManaBase3 * player.manaCost);
            if ((item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.Starfury) & player.statMana < manaCost1)
            {
                return false;
            }
            if ((item.type == ItemID.LightsBane | item.type == ItemID.BladeofGrass) & player.statMana < manaCost2)
            {
                return false;
            }
            if ((item.type == ItemID.Meowmere | item.type == ItemID.StarWrath) & player.statMana < manaCost3)
            {
                return false;
            }
            return true;
        }
        public override bool? UseItem(Item item, Player player)
        {
            if (item.DamageType != DamageClass.Magic && item.mana > 0)
            {
                player.manaRegenDelay = ManaDelay;
                return true;
            }
            return null;
        }
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ModContent.ItemType<AncientGoldenHelmet>() && body.type == ItemID.Gi && legs.type == ModContent.ItemType<AncientGoldenGreaves>())
            {
                return "GoldenGi";
            }
            else return base.IsArmorSet(head, body, legs);
        }
        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "GoldenGi")
            {
                player.setBonus = "Increases melee damage by 3 flat";

                player.GetDamage(DamageClass.Melee).Flat += 3f;
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.player[Main.myPlayer];
            int manaCost1 = (int)(ManaBase1 * player.manaCost);
            int manaCost2 = (int)(ManaBase2 * player.manaCost);
            int manaCost3 = (int)(ManaBase3 * player.manaCost);
            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.Starfury)
            {
                tooltips.Insert(6, new TooltipLine(Mod, "", "Requires " + manaCost1 + " mana to cast projectile"));
            }
            if (item.type == ItemID.LightsBane | item.type == ItemID.BladeofGrass)
            {
                tooltips.Insert(6, new TooltipLine(Mod, "", "Requires " + manaCost2 + " mana to cast projectile"));
            }
            if (item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                tooltips.Insert(5, new TooltipLine(Mod, "", "Requires " + manaCost3 + " mana to cast projectile"));
            }
        }
    }
}
