using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;

namespace tsorcRevamp.Items.VanillaItems
{
    class MeleeEdits : GlobalItem
    {
        public static int ManaBase1 = 40;
        public static int ManaBase2 = 20;
        public static int ManaBase3 = 50;
        public static int ManaDelay = 720;
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.DayBreak)
            {
                item.mana = 88;
            }
            if (item.type == ItemID.Terrarian)
            {
                item.mana = 40;
            }
            if (item.type == ItemID.NightsEdge)
            {
                item.mana = 25;
            }
            if (item.type == ItemID.TrueNightsEdge)
            {
                item.damage = 85;
                item.mana = 35;
            }
            if (item.type == ItemID.Excalibur)
            {
                item.mana = 24;
            }
            if (item.type == ItemID.TrueExcalibur)
            {
                item.damage = 75;
                item.mana = 30;
            }
            if (item.type == ItemID.TerraBlade)
            {
                item.mana = 40;
            }
            if (item.type == ItemID.TheHorsemansBlade)
            {
                item.damage = 200;
                item.useTime = 20;
                item.useAnimation = 20;
                item.mana = 25;
            }
            if (item.type == ItemID.PiercingStarlight)
            {
                item.damage = 50;
            }
        }
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int manaCost1 = (int)(ManaBase1 * player.manaCost);
            int manaCost2 = (int)(ManaBase2 * player.manaCost);
            int manaCost3 = (int)(ManaBase3 * player.manaCost);

            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.Starfury)
            {
                player.statMana -= manaCost1;
                player.manaRegenDelay = ManaDelay;
            }
            if (item.type == ItemID.LightsBane | item.type == ItemID.BladeofGrass)
            {
                player.statMana -= manaCost2;
                player.manaRegenDelay = ManaDelay;
            }
            if (item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                player.statMana -= manaCost3;
                player.manaRegenDelay = ManaDelay;
            }
            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
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
        public static int GoldenGiFlatDamage = 3;
        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "GoldenGi")
            {
                player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.GoldenGi");

                player.GetDamage(DamageClass.Melee).Flat += GoldenGiFlatDamage;
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
                tooltips.Insert(6, new TooltipLine(Mod, "ManaCost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaCost1") + manaCost1 + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaCost2")));
            }
            if (item.type == ItemID.LightsBane | item.type == ItemID.BladeofGrass)
            {
                tooltips.Insert(6, new TooltipLine(Mod, "ManaCost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaCost1") + manaCost2 + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaCost2")));
            }
            if (item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                tooltips.Insert(5, new TooltipLine(Mod, "ManaCost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaCost1") + manaCost3 + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaCost2")));
            }
        }
    }
}
