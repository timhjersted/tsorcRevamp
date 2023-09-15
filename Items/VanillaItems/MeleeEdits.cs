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
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class MeleeEdits : GlobalItem
    {
        public static int ManaDelay = 720;
        public override void SetDefaults(Item item)
        {
            SetMeleeSlashColor(item);

            if (item.type == ItemID.WoodenBoomerang)
            {
                item.mana = 5;
            }
            if (item.type == ItemID.EnchantedBoomerang)
            {
                item.mana = 7;
            }
            if (item.type == ItemID.Shroomerang)
            {
                item.mana = 7;
            }
            if (item.type == ItemID.IceBoomerang)
            {
                item.mana = 6;
            }
            if (item.type == ItemID.Trimarang)
            {
                item.mana = 6;
            }
            if (item.type == ItemID.IceBlade)
            {
                item.mana = 8;
            }
            if (item.type == ItemID.EnchantedSword)
            {
                item.mana = 9;
            }
            if (item.type == ItemID.FruitcakeChakram)
            {
                item.mana = 5;
            }
            if (item.type == ItemID.BloodyMachete)
            {
                item.mana = 4;
            }
            if (item.type == ItemID.ThunderSpear)
            {
                item.mana = 7;
            }
            if (item.type == ItemID.Starfury)
            {
                item.mana = 10;
            }
            if (item.type == ItemID.LightsBane)
            {
                item.mana = 9;
            }
            if (item.type == ItemID.BladeofGrass)
            {
                item.mana = 13;
            }
            if (item.type == ItemID.ThornChakram)
            {
                item.mana = 8;
            }
            if (item.type == ItemID.CombatWrench)
            {
                item.mana = 9;
            }
            if (item.type == ItemID.Flamarang)
            {
                item.mana = 10;
            }
            if (item.type == ItemID.NightsEdge)
            {
                item.mana = 20;
            }
            if (item.type == ItemID.FlyingKnife)
            {
                item.mana = 20;
            }
            if (item.type == ItemID.BeamSword)
            {
                item.mana = 15;
            }
            if (item.type == ItemID.Frostbrand)
            {
                item.mana = 14;
            }
            if (item.type == ItemID.Bananarang)
            {
                item.mana = 14;
            }
            if (item.type == ItemID.LightDisc)
            {
                item.mana = 16;
            }
            if (item.type == ItemID.BouncingShield)
            {
                item.mana = 22;
            }
            if (item.type == ItemID.Excalibur)
            {
                item.mana = 20;
            }
            if (item.type == ItemID.TrueExcalibur)
            {
                item.damage = 75;
                item.mana = 26;
            }
            if (item.type == ItemID.TrueNightsEdge)
            {
                item.damage = 85;
                item.mana = 33;
            }
            if (item.type == ItemID.PossessedHatchet)
            {
                item.mana = 20;
            }
            if (item.type == ItemID.PaladinsHammer)
            {
                item.mana = 20;
            }
            if (item.type == ItemID.TerraBlade)
            {
                item.mana = 35;
            }
            if (item.type == ItemID.TheHorsemansBlade)
            {
                item.damage = 200;
                item.useTime = 20;
                item.useAnimation = 20;
                item.mana = 28;
            }
            if (item.type == ItemID.DD2SquireBetsySword)
            {
                item.mana = 22;
            }
            if (item.type == ItemID.PiercingStarlight)
            {
                item.damage = 50;
            }
            if (item.type == ItemID.VampireKnives)
            {
                item.mana = 35;
            }
            if (item.type == ItemID.ScourgeoftheCorruptor)
            {
                item.mana = 17;
            }
            if (item.type == ItemID.DayBreak)
            {
                item.mana = 40;
            }
            if (item.type == ItemID.Terrarian)
            {
                item.mana = 50;
            }
            if (item.type == ItemID.Meowmere)
            {
                item.mana = 50;
            }
            if (item.type == ItemID.StarWrath)
            {
                item.mana = 60;
            }
        }

        public static void SetMeleeSlashColor(Item item)
        {
            tsorcInstancedGlobalItem instancedGlobal = item.GetGlobalItem<tsorcInstancedGlobalItem>();
            switch (item.type)
            {
                case ItemID.IronBroadsword:
                case ItemID.IronHammer:
                case ItemID.IronAxe:
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
                case ItemID.BluePhasesaber:
                    instancedGlobal.slashColor = Color.Blue;
                    break;
                case ItemID.RedPhaseblade:
                case ItemID.RedPhasesaber:
                    instancedGlobal.slashColor = Color.Red;
                    break;
                case ItemID.GreenPhaseblade:
                case ItemID.GreenPhasesaber:
                    instancedGlobal.slashColor = Color.Green;
                    break;
                case ItemID.PurplePhaseblade:
                case ItemID.PurplePhasesaber:
                    instancedGlobal.slashColor = Color.Purple;
                    break;
                case ItemID.WhitePhaseblade:
                case ItemID.WhitePhasesaber:
                    instancedGlobal.slashColor = Color.WhiteSmoke;
                    break;
                case ItemID.YellowPhaseblade:
                case ItemID.YellowPhasesaber:
                    instancedGlobal.slashColor = Color.Yellow;
                    break;
                case ItemID.MeteorHamaxe:
                    instancedGlobal.slashColor = Color.Purple;
                    break;
                case ItemID.MoltenHamaxe:
                    instancedGlobal.slashColor = Color.DarkOrange;
                    break;
                case ItemID.AdamantiteSword:
                case ItemID.AdamantiteWaraxe:
                    instancedGlobal.slashColor = Color.Red;
                    break;
                case ItemID.CobaltSword:
                case ItemID.CobaltWaraxe:
                    instancedGlobal.slashColor = Color.RoyalBlue;
                    break;
                case ItemID.MythrilSword:
                case ItemID.MythrilWaraxe:
                    instancedGlobal.slashColor = Color.PaleGreen;
                    break;
                case ItemID.EnchantedSword:
                    instancedGlobal.slashColor = Color.DarkSlateBlue;
                    break;
                case ItemID.BeeKeeper:
                    instancedGlobal.slashColor = Color.MediumPurple;
                    break;
                case ItemID.PalladiumSword:
                case ItemID.PalladiumWaraxe:
                    instancedGlobal.slashColor = Color.DarkOrange;
                    break;
                case ItemID.OrichalcumSword:
                case ItemID.OrichalcumWaraxe:
                    instancedGlobal.slashColor = Color.HotPink;
                    break;
                case ItemID.ChlorophyteClaymore:
                case ItemID.ChlorophyteSaber:
                case ItemID.ChlorophyteGreataxe:
                case ItemID.ChlorophyteWarhammer:
                    instancedGlobal.slashColor = Color.LawnGreen;
                    break;
                case ItemID.ZombieArm:
                    instancedGlobal.slashColor = Color.DarkOliveGreen;
                    break;
                case ItemID.TheAxe:
                    instancedGlobal.slashColor = Color.Red;
                    break;
                case ItemID.IceSickle:
                    instancedGlobal.slashColor = Color.DeepSkyBlue;
                    break;
                case ItemID.DeathSickle:
                    instancedGlobal.slashColor = Color.DarkViolet;
                    break;
                case ItemID.SpectreHamaxe:
                    instancedGlobal.slashColor = Color.CornflowerBlue;
                    break;
                case ItemID.CandyCaneSword:
                    instancedGlobal.slashColor = Color.Red;
                    break;
                case ItemID.ChristmasTreeSword:
                    instancedGlobal.slashColor = Color.ForestGreen;
                    break;
                case ItemID.PurpleClubberfish:
                    instancedGlobal.slashColor = Color.BlueViolet;
                    break;
                case ItemID.PalmWoodHammer:
                case ItemID.PalmWoodSword:
                    instancedGlobal.slashColor = Color.PeachPuff; //who is naming these colors?
                    break;
                case ItemID.BorealWoodSword:
                case ItemID.BorealWoodHammer:
                    instancedGlobal.slashColor = Color.BurlyWood; //"Please paint my walls Burlywood"
                    break;
                case ItemID.InfluxWaver:
                    instancedGlobal.slashColor = Color.DarkCyan;
                    break;
                case ItemID.FetidBaghnakhs:
                    instancedGlobal.slashColor = Color.BurlyWood; 
                    break;
                case ItemID.Seedler:
                    instancedGlobal.slashColor = Color.YellowGreen;
                    break;
                case ItemID.Meowmere:
                    instancedGlobal.slashColor = Color.Coral;
                    break;
                case ItemID.StarWrath:
                    instancedGlobal.slashColor = Color.DeepPink;
                    break;
                case ItemID.PsychoKnife:
                    instancedGlobal.slashColor = Color.Red;
                    break;
                case ItemID.Bladetongue:
                    instancedGlobal.slashColor = Color.Pink;
                    break;
                case ItemID.SlapHand:
                    instancedGlobal.slashColor = Color.BurlyWood;
                    break;
                case ItemID.PlatinumHammer:
                case ItemID.PlatinumAxe:
                case ItemID.PlatinumBroadsword:
                    instancedGlobal.slashColor = Color.CornflowerBlue;
                    break;
                case ItemID.TungstenHammer:
                case ItemID.TungstenAxe:
                case ItemID.TungstenBroadsword:
                    instancedGlobal.slashColor = Color.SpringGreen;
                    break;
                case ItemID.LeadHammer:
                case ItemID.LeadAxe:
                case ItemID.LeadBroadsword:
                    instancedGlobal.slashColor = Color.MidnightBlue;
                    break;
                case ItemID.TinHammer:
                case ItemID.TinAxe:
                case ItemID.TinBroadsword:
                    instancedGlobal.slashColor = Color.Khaki;
                    break;
                case ItemID.CopperHammer:
                case ItemID.CopperAxe:
                case ItemID.CopperBroadsword:
                    instancedGlobal.slashColor = Color.Peru; //"my favorite color is Peru" -- statements dreamed up by the utterly deranged
                    break;
                case ItemID.SilverHammer:
                case ItemID.SilverAxe:
                case ItemID.SilverBroadsword:
                    instancedGlobal.slashColor = Color.Silver;
                    break;
                case ItemID.GoldHammer:
                case ItemID.GoldAxe:
                case ItemID.GoldBroadsword:
                    instancedGlobal.slashColor = Color.DarkGoldenrod;
                    break;
                case ItemID.SolarFlareAxe:
                case ItemID.SolarFlareHammer:
                case ItemID.LunarHamaxeSolar:
                    instancedGlobal.slashColor = Color.DarkOrange;
                    break;
                case ItemID.VortexAxe:
                case ItemID.VortexHammer:
                case ItemID.LunarHamaxeVortex:
                    instancedGlobal.slashColor = Color.MediumSeaGreen;
                    break;
                case ItemID.NebulaAxe:
                case ItemID.NebulaHammer:
                case ItemID.LunarHamaxeNebula:
                    instancedGlobal.slashColor = Color.Magenta;
                    break;
                case ItemID.StardustAxe:
                case ItemID.StardustHammer:
                case ItemID.LunarHamaxeStardust:
                    instancedGlobal.slashColor = Color.Teal;
                    break;
                case ItemID.AntlionClaw:
                    instancedGlobal.slashColor = Color.Gold;
                    break;
                case ItemID.DD2SquireDemonSword:
                    instancedGlobal.slashColor = Color.MediumVioletRed;
                    break;
                case ItemID.DD2SquireBetsySword:
                    instancedGlobal.slashColor = Color.DarkSalmon;
                    break;
                case ItemID.OrangePhaseblade:
                case ItemID.OrangePhasesaber:
                    instancedGlobal.slashColor = Color.Orange;
                    break;
                case ItemID.BloodHamaxe:
                    instancedGlobal.slashColor = Color.PaleVioletRed;
                    break;
                case ItemID.GravediggerShovel:
                    instancedGlobal.slashColor = Color.Gray;
                    break;
                case ItemID.TentacleSpike:
                    instancedGlobal.slashColor = Color.DarkViolet;
                    break;
                case ItemID.LucyTheAxe:
                    instancedGlobal.slashColor = Color.Red;
                    break;
                case ItemID.HamBat:
                    instancedGlobal.slashColor = Color.IndianRed;
                    break;
                case ItemID.BatBat:
                    instancedGlobal.slashColor = Color.PaleVioletRed;
                    break;
                case ItemID.Flymeal:
                    instancedGlobal.slashColor = Color.DarkOliveGreen;
                    break;
                case ItemID.AshWoodHammer:
                case ItemID.AshWoodSword:
                    instancedGlobal.slashColor = Color.DarkOrange;
                    break;
                case ItemID.WaffleIron:
                    instancedGlobal.slashColor = Color.Gray;
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
            if (head.type == ItemID.ChlorophyteMask && body.type == ItemID.ChlorophytePlateMail && legs.type == ItemID.ChlorophyteGreaves)
            {
                return "ChlorophyteMask";
            }
            if (head.type == ItemID.TurtleHelmet && body.type == ItemID.TurtleScaleMail && legs.type == ItemID.TurtleLeggings)
            {
                return "TurtleSet";
            }
            else return base.IsArmorSet(head, body, legs);
        }
        public static int GoldenGiFlatDamage = 3;
        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "GoldenGi")
            {
                player.setBonus = LangUtils.GetTextValue("Items.VanillaItems.GoldenGi", GoldenGiFlatDamage);

                player.GetDamage(DamageClass.Melee).Flat += GoldenGiFlatDamage;
            }
            if (set == "ChlorophyteMask")
            {
                player.setBonus = LangUtils.GetTextValue("Items.VanillaItems.ChlorophyteMaskSet", 5);
            }
            if (set == "TurtleSet")
            {
                player.setBonus = LangUtils.GetTextValue("Items.VanillaItems.TurtleSet", 15);
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.player[Main.myPlayer];
        }
    }
}
