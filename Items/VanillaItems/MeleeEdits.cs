using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class MeleeEdits : GlobalItem
    {
        public static int ManaDelay = 480; // was 540
        public override void SetDefaults(Item item)
        {
            SetMeleeSlashColor(item);
            if (item.type == ItemID.BoneSword || item.type == ItemID.Bladetongue || item.type == ItemID.BloodButcherer || item.type == ItemID.Muramasa || item.type == ItemID.BeamSword
                || item.type == ItemID.Frostbrand || item.type == ItemID.BeeKeeper || item.type == ItemID.PalladiumSword || item.type == ItemID.OrichalcumSword || item.type == ItemID.InfluxWaver
                || item.type == ItemID.Meowmere || item.type == ItemID.StarWrath)
            {
                item.width = 50;
                item.height = 50;
            }
            if (item.type == ItemID.FalconBlade)
            {
                item.width = 36;
                item.height = 40;
            }
            // Phaseblades
            if (item.type == ItemID.BluePhaseblade || item.type == ItemID.GreenPhaseblade || item.type == ItemID.YellowPhaseblade || item.type == ItemID.RedPhaseblade
                || item.type == ItemID.WhitePhaseblade || item.type == ItemID.PurplePhaseblade || item.type == ItemID.OrangePhaseblade)
            {
                item.width = 48;
                item.height = 48;
                item.damage = 36;
                //until someone can edit the recipe for phaseblades, I'm just nerfing their damage so they don't make everything else obsolete in the same progression tier
            }
            if (item.type == ItemID.BluePhasesaber || item.type == ItemID.GreenPhasesaber || item.type == ItemID.YellowPhasesaber || item.type == ItemID.RedPhasesaber
                || item.type == ItemID.WhitePhasesaber || item.type == ItemID.PurplePhasesaber || item.type == ItemID.OrangePhasesaber || item.type == ItemID.AdamantiteSword
                || item.type == ItemID.CobaltSword || item.type == ItemID.MythrilSword || item.type == ItemID.Keybrand || item.type == ItemID.TitaniumSword
                || item.type == ItemID.DeathSickle || item.type == ItemID.DD2SquireDemonSword)
            {
                item.width = 56;
                item.height = 56;
            }
            if (item.type == ItemID.BloodLustCluster)
            {
                item.width = 58;
                item.height = 48;
            }
            if (item.type == ItemID.FieryGreatsword || item.type == ItemID.DD2SquireBetsySword)
            {
                item.width = 70;
                item.height = 70;
            }
            if (item.type == ItemID.Katana)
            {
                item.width = 48;
                item.height = 54;
            }
            if (item.type == ItemID.IronBroadsword || item.type == ItemID.IceBlade)
            {
                item.width = 36;
                item.height = 36;
            }
            if (item.type == ItemID.TungstenBroadsword || item.type == ItemID.SilverBroadsword)
            {
                item.width = 38;
                item.height = 38;
            }
            if (item.type == ItemID.GoldBroadsword || item.type == ItemID.Cutlass || item.type == ItemID.IceSickle)
            {
                item.width = 40;
                item.height = 40;
            }
            if (item.type == ItemID.WoodenBoomerang)
            {
                //item.mana = 0;
            }
            if (item.type == ItemID.EnchantedBoomerang)
            {
                //item.mana = 5;
            }
            if (item.type == ItemID.Shroomerang)
            {
                //item.mana = 5;
            }
            if (item.type == ItemID.IceBoomerang)
            {
                //item.mana = 4;
            }
            if (item.type == ItemID.FruitcakeChakram)
            {
                //item.mana = 3;
            }
            if (item.type == ItemID.BloodyMachete)
            {
                //item.mana = 2;
            }
            if (item.type == ItemID.Trimarang)
            {
                //item.mana = 4;
            }
            if (item.type == ItemID.IceBlade)
            {
                item.shootsEveryUse = true;
                //item.mana = 5;
            }
            if (item.type == ItemID.EnchantedSword)
            {
                item.shootsEveryUse = true;
                //item.mana = 7;
            }
            if (item.type == ItemID.ThunderSpear)
            {
                //item.mana = 3;
            }
            if (item.type == ItemID.Starfury)
            {
                item.shootsEveryUse = true;
                //item.mana = 8;
            }
            if (item.type == ItemID.LightsBane)
            {
                item.width = 50;
                item.height = 50;
            }
            if (item.type == ItemID.BladeofGrass)
            {
                //item.mana = 5;
                item.width = 68;
                item.height = 68;
            }
            if (item.type == ItemID.ThornChakram)
            {
                //item.mana = 6;
            }
            if (item.type == ItemID.CombatWrench)
            {
                //item.mana = 7;
            }
            if (item.type == ItemID.Flamarang)
            {
                //item.mana = 8;
            }
            if (item.type == ItemID.FlyingKnife)
            {
                //item.mana = 18;
            }
            if (item.type == ItemID.BeamSword)
            {
                item.shootsEveryUse = true;
                //item.mana = 10;
            }
            if (item.type == ItemID.Frostbrand)
            {
                item.shootsEveryUse = true;
                //item.mana = 12;
            }
            if (item.type == ItemID.Bananarang)
            {
                //item.mana = 12;
            }
            if (item.type == ItemID.ShadowFlameKnife)
            {
                //item.mana = 10;
            }
            if (item.type == ItemID.LightDisc)
            {
                //item.mana = 11;
            }
            if (item.type == ItemID.BouncingShield)
            {
                //item.mana = 15;
            }
            if (item.type == ItemID.ChlorophyteSaber)
            {
                item.shootsEveryUse = true;
                //item.mana = 14;
            }
            if (item.type == ItemID.ChlorophyteClaymore)
            {
                item.width = 66;
                item.height = 66;
                item.shootsEveryUse = true;
                //item.mana = 17;
            }
            if (item.type == ItemID.TrueNightsEdge)
            {
                item.damage = 85;
                //item.mana = 18;
            }
            if (item.type == ItemID.Seedler)
            {
                item.shootsEveryUse = true;
                item.width = 48;
                item.height = 48;
                //item.mana = 11;
            }
            if (item.type == ItemID.PossessedHatchet)
            {
                item.damage = 110; //vanilla 80
                //item.mana = 12;
            }
            if (item.type == ItemID.Flairon)
            {
                //item.mana = 17;
            }
            if (item.type == ItemID.GolemFist)
            {
                item.damage = 125; //vanilla 90
            }
            if (item.type == ItemID.PaladinsHammer)
            {
                //item.mana = 15;
            }
            if (item.type == ItemID.TerraBlade)
            {
                //item.mana = 23;
            }
            if (item.type == ItemID.TheHorsemansBlade)
            {
                item.damage = 200;
                item.useTime = 20;
                item.useAnimation = 20;
                //item.mana = 16;
            }
            if (item.type == ItemID.DD2SquireBetsySword)
            {
                //item.mana = 18;
            }
            if (item.type == ItemID.PiercingStarlight)
            {
                item.damage = 35;
            }
            if (item.type == ItemID.VampireKnives)
            {
                item.damage = 20;
                //item.mana = 31;
            }
            if (item.type == ItemID.ScourgeoftheCorruptor)
            {
                //item.mana = 14;
            }
            if (item.type == ItemID.InfluxWaver)
            {
                item.shootsEveryUse = true;
                //item.mana = 23;
            }
            if (item.type == ItemID.DayBreak)
            {
                //item.mana = 28;
            }
            if (item.type == ItemID.Terrarian)
            {
                //item.mana = 31;
            }
            if (item.type == ItemID.Meowmere)
            {
                //item.mana = 31;
            }
            if (item.type == ItemID.StarWrath)
            {
                //item.mana = 34;
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
            if ((item.DamageType != DamageClass.Magic && item.mana > 0) || VanillaProjectileMelee(item))
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
            if (head.type == ItemID.SolarFlareHelmet && body.type == ItemID.SolarFlareBreastplate && legs.type == ItemID.SolarFlareLeggings)
            {
                return "SolarSet";
            }
            else return base.IsArmorSet(head, body, legs);
        }
        public static int GoldenGiFlatDamage = 3;
        public const float SolarSetDR = 14f;
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
                player.setBonus = LangUtils.GetTextValue("Items.VanillaItems.TurtleSet", 15 + MinorEdits.TurtleSetResistBonus);
            }
            if (set == "SolarSet")
            {
                player.setBonus += "\n" + LangUtils.GetTextValue("CommonItemTooltip.DRStat", SolarSetDR);
                player.endurance += (SolarSetDR - 12f) / 100f;
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.player[Main.myPlayer];
            if (item.type == ItemID.Swordfish)
            {
                tooltips.Add(new TooltipLine(Mod, "BOTCNoHeal", LangUtils.GetTextValue("CommonItemTooltip.Trident")));
            }
            if (item.type == ItemID.ObsidianSwordfish)
            {
                tooltips.Add(new TooltipLine(Mod, "BOTCNoHeal", LangUtils.GetTextValue("CommonItemTooltip.Trident")));
            }
            if (VanillaProjectileMelee(item))
            {
                tooltips.Add(new TooltipLine(Mod, "ManaCost", Language.GetTextValue("CommonItemTooltip.UsesMana", GetMeleeManaCost(item, player))));
            }
        }
        public override void HoldItem(Item item, Player player)
        {
            if (item.type == ItemID.Swordfish)
            {
                player.accFlipper = true;
                player.ignoreWater = true;
            }
            if (item.type == ItemID.ObsidianSwordfish)
            {
                player.accFlipper = true;
                player.ignoreWater = true;
                player.AddBuff(BuffID.ObsidianSkin, 1);
            }
            if (VanillaProjectileMelee(item) && player.HasItem(ModContent.ItemType<Items.Debug.DebugTome>()))
            {
                Main.NewText("This item is VanillaProjectileMelee");
                return;
            }
        }
        public int GetMeleeManaCost(Item item, Player player)
        {
            int baseCost = (int)MathHelper.Clamp((item.damage * 60 / item.useTime) * 1 / 4, 0, 25);//edit numbers here if u wnat to adjust mana cost
            int finalCost = 0;
            if (tsorcRevampWorld.SuperHardMode)
            {
                baseCost += 10;
            }
            finalCost = (int)(baseCost * player.manaCost);
            return finalCost;
        }
        public bool VanillaProjectileMelee(Item item)
        {
            //what a miracle that vanilla melee code don't crash
            Projectile Judge = new();
            Judge.SetDefaults(item.shoot);
            if (item.DamageType == DamageClass.Melee && item.type < ItemID.Count
                && Judge.aiStyle != ProjAIStyleID.Spear
                && item.shoot != 0
                || tsorcRevamp.VanillaMeleeBlackList.Contains(item.type)
                //|| Judge.aiStyle == ProjAIStyleID.Flail
                //|| Judge.aiStyle == ProjAIStyleID.Boomerang
                || Judge.aiStyle == ProjAIStyleID.Yoyo)
            {
                return true;
            }
            return false;
        }
        public override bool CanUseItem(Item item, Player player)
        {
            if (VanillaProjectileMelee(item) && player.statMana < GetMeleeManaCost(item, player))
            {
                return false;
            }
            return  true;
        }
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //mercy, yoyo only use mana once when left click to unleash 
            if (VanillaProjectileMelee(item) && player.statMana >= GetMeleeManaCost(item, player))
            {
                player.statMana -= GetMeleeManaCost(item, player);
            }
            return true;
        }
        public bool TrueMelee(Item item)
        {
            if (item.DamageType == DamageClass.Melee && item.shoot == 0)
            {
                return true;
            }
            return false;
        }
        public void ReboundProjectile(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile Proj = Main.projectile[i];
                if (Proj.active && Proj.hostile && Proj.damage < player.HeldItem.damage)
                {
                    Rectangle ProjBox = Proj.getRect();
                    if (ProjBox.Intersects(player.HeldItem.Hitbox))
                    {
                        Vector2 boi = Main.MouseWorld - Proj.Center;
                        boi.Normalize();
                        Proj.hostile = false;
                        Proj.friendly = true;
                        Proj.velocity = boi * Proj.velocity.Length();
                    }
                }
            }
        }
    }
}
