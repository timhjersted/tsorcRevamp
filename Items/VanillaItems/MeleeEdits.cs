using Humanizer;
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
            SetMeleeSlashColor(item);

            if (item.type == ItemID.DayBreak)
            {
                item.mana = 88;
            }
            if (item.type == ItemID.Terrarian)
            {
                item.mana = ManaBase1;
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
                item.mana = ManaBase1;
            }
            if (item.type == ItemID.TheHorsemansBlade)
            {
                item.damage = 200;
                item.useTime = 20;
                item.useAnimation = 20;
                item.mana = 25;
            }
            if (item.type == ItemID.VampireKnives)
            {
                item.mana = 35;
            }
            if (item.type == ItemID.IceBlade | item.type == ItemID.EnchantedSword | item.type == ItemID.BeamSword | item.type == ItemID.Frostbrand | item.type == ItemID.Starfury)
            {
                item.mana = ManaBase1;
            }
            if (item.type == ItemID.LightsBane | item.type == ItemID.BladeofGrass)
            {
                item.mana = ManaBase2;
            }
            if (item.type == ItemID.Meowmere | item.type == ItemID.StarWrath)
            {
                item.mana = ManaBase3;
            }
            if (item.type == ItemID.PiercingStarlight)
            {
                item.damage = 50;
            }
        }

        public static void SetMeleeSlashColor(Item item)
        {
            tsorcInstancedGlobalItem instancedGlobal = item.GetGlobalItem<tsorcInstancedGlobalItem>();
            switch (item.type)
            {
                case ItemID.IronBroadsword:
                    instancedGlobal.slashColor = Color.SlateGray;
                    break;
                case ItemID.WoodenSword:
                    instancedGlobal.slashColor = Color.Brown;
                    break;
                case ItemID.WarAxeoftheNight:
                    instancedGlobal.slashColor = Color.DarkMagenta;
                    break;
                case ItemID.LightsBane:
                    instancedGlobal.slashColor = Color.DarkMagenta;
                    break;
                case ItemID.Starfury:
                    instancedGlobal.slashColor = Color.Pink;
                    break;
                case ItemID.TheBreaker:
                    instancedGlobal.slashColor = Color.DarkMagenta;
                    break;
                case ItemID.FieryGreatsword:
                    instancedGlobal.slashColor = Color.OrangeRed;
                    break;
                case ItemID.Muramasa:
                    instancedGlobal.slashColor = Color.Blue;
                    break;
                case ItemID.BladeofGrass:
                    instancedGlobal.slashColor = Color.YellowGreen;
                    break;
                case ItemID.WoodenHammer:
                    instancedGlobal.slashColor = Color.Brown;
                    break;
                case ItemID.BluePhaseblade:
                    instancedGlobal.slashColor = Color.Blue;
                    break;
                case ItemID.RedPhaseblade:
                    instancedGlobal.slashColor = Color.Red;
                    break;
            }
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
                player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.GoldenGi").FormatWith(GoldenGiFlatDamage);

                player.GetDamage(DamageClass.Melee).Flat += GoldenGiFlatDamage;
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.player[Main.myPlayer];
        }
    }
}
