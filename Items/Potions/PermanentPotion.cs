using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Items.Potions.PermanentPotions
{
    //memory management is scary
    public abstract class PermanentPotion : ModItem
    {
        public static readonly List<PermanentPotion> ExclusiveSetCombat = new() {
            new PermanentArmorDrug(),
            new PermanentDemonDrug(),
            new PermanentStrengthPotion(),
            new PermanentBattlefrontPotion()
        };

        public static readonly List<PermanentPotion> ExclusiveSetWellFed = new() {
            new PermanentTea(),
            new PermanentSoup(),
            new PermanentGoldenDelight(),
        };

        public static readonly List<PermanentPotion> ExclusiveSetFlasks = new() {
            new PermanentFlaskOfCursedFlames(),
            new PermanentFlaskOfFire(),
            new PermanentFlaskOfGold(),
            new PermanentFlaskOfIchor(),
            new PermanentFlaskOfNanites(),
            new PermanentFlaskOfParty(),
            new PermanentFlaskOfPoison(),
            new PermanentFlaskOfVenom()
        };
        public abstract int PermanentID
        {
            get;
        }

        public abstract int BuffType {
            get;
        }

        public string BuffName {
            get {
                //thank you online regex builders
                Regex splitAtCapitals = new (@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                string name = $"{GetType().Name}";
                name = splitAtCapitals.Replace(name, " ");
                name = name.Replace("Permanent", "");
                name = name.Replace("Potion", "");
                return name;
            }
        }

        public int ConsumedAmount {
            get {
                //this all seems a bit scary to do every frame but i cant think of a better way ;-;
                tsorcRevampPlayer modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
                int consumedAmount = 0;
                if (modPlayer.consumedPotions.Count > 0) {
                    foreach (ItemDefinition def in modPlayer.consumedPotions.Keys) {
                        int itemID = def.Type;
                        Item potion = new();
                        potion.SetDefaults(itemID);
                        if (potion.buffType == BuffType) {
                            consumedAmount = modPlayer.consumedPotions[def];
                            break;
                        }
                    }
                }
                return consumedAmount;
            }
        }

        public virtual bool CanScale {
            get => false;
        }
        public virtual int ScalingFactor {
            get => 150;
        }

        public virtual float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.5f;
                return Math.Min(potency, 1.5f);
            }
        }
        public virtual List<PermanentPotion> ExclusivePermanents {
            get;
        }
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 25;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item21;
            Item.rare = ItemRarityID.Orange;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tsorcRevampPlayer modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            int ttindex = tooltips.FindLastIndex(t => t.Mod != null);
            if (ttindex != -1)
            {
                int line = ttindex;
                line++;
                if (CanScale) {
                    //spaces intentionally missing before and after {BuffName}
                    tooltips.Insert(line++, new TooltipLine(Mod, "", $"[c/77ff77:Grants a weaker{BuffName}effect which increases]"));
                    tooltips.Insert(line++, new TooltipLine(Mod, "", $"[c/77ff77:increases in strength with each non-permanent]"));
                    tooltips.Insert(line++, new TooltipLine(Mod, "", $"[c/77ff77:source of{BuffName}consumed. Total consumed:] {ConsumedAmount}"));
                    tooltips.Insert(line++, new TooltipLine(Mod, "", $"[c/77ff77:Effect potency is ]{EffectPotency * 100:F2}%"));

                }
                else {
                    tooltips.Insert(line++, new TooltipLine(Mod, "", $"Permanently grants the{BuffName}buff."));
                }
                tooltips.Insert(line++, new TooltipLine(Mod, "", "Does not consume a buff slot."));
                tooltips.Insert(line++, new TooltipLine(Mod, "", "Use to toggle effect."));
                if (ExclusivePermanents.Equals(ExclusiveSetFlasks)) {
                    tooltips.Insert(line++, new TooltipLine(Mod, "", "Not compatible with other weapon imbues."));

                }
                else if (ExclusivePermanents.Equals(ExclusiveSetWellFed)) {
                    tooltips.Insert(line++, new TooltipLine(Mod, "", "Not compatible with other food items."));

                }

                tooltips.Insert(line++, new TooltipLine(Mod, "", "[c/ff7777:Does nothing while the non-permanent buff is active.]"));
            }
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[PermanentID] = !modPlayer.PermanentBuffToggles[PermanentID];

            if (ExclusivePermanents != null) {
                foreach (PermanentPotion checkID in ExclusivePermanents) {
                    //dont disable self
                    if (checkID.PermanentID == PermanentID)
                        continue;
                    //toggle off
                    modPlayer.PermanentBuffToggles[checkID.PermanentID] = false;
                }
            }
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[PermanentID])
            {
                Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.PermanentBuffToggles[PermanentID] && !modPlayer.ActivePermanentPotions.Contains(PermanentID))
            {
                modPlayer.ActivePermanentPotions.Add(PermanentID);
                bool canGiveEffect = true;
                if (player.HasBuff(BuffType)) {
                    canGiveEffect = false;
                }

                if (!canGiveEffect) return;
                if (ExclusivePermanents != null) {
                    foreach (PermanentPotion checkID in ExclusivePermanents) {
                        if (checkID.PermanentID == PermanentID) continue;
                        if (player.HasBuff(checkID.BuffType)) canGiveEffect = false;
                    }
                }

                if (canGiveEffect) PotionEffect(player);
            }
        }

        public abstract void PotionEffect(Player player);
        public float ApplyScaling(float value) {
            return value * EffectPotency;
        }
    }
    public class PermanentObsidianSkinPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_288";
        public override int PermanentID => 0;
        public override int BuffType => BuffID.ObsidianSkin;
        public override void PotionEffect(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
        }
    }

    public class PermanentRegenerationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_289";
        public override int PermanentID => 1;
        public override int BuffType => BuffID.Regeneration;
        public override bool CanScale => true;
        public override void PotionEffect(Player player)
        {
            player.lifeRegen += (int)ApplyScaling(4);
        }
    }

    public class PermanentSwiftnessPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_290";
        public override int PermanentID => 2;
        public override int BuffType => BuffID.Swiftness;
        public override bool CanScale => true;
        public override void PotionEffect(Player player)
        {
            player.moveSpeed += ApplyScaling(0.25f);
        }
    }
    public class PermanentGillsPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_291";
        public override int PermanentID => 3;
        public override int BuffType => BuffID.Gills;
        public override void PotionEffect(Player player)
        {
            player.gills = true;
        }
    }
    public class PermanentIronskinPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_292";
        public override int PermanentID => 4;
        public override int BuffType => BuffID.Ironskin;
        public override bool CanScale => true;
        public override void PotionEffect(Player player)
        {
            player.statDefense += (int)ApplyScaling(8f);
        }
    }
    public class PermanentManaRegenerationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_293";
        public override int PermanentID => 5;
        public override int BuffType => BuffID.ManaRegeneration;
        public override void PotionEffect(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().manaShield == 0)
            {
                player.manaRegenBuff = true;
            }
        }
    }
    public class PermanentMagicPowerPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_294";
        public override int PermanentID => 6;
        public override int BuffType => BuffID.MagicPower;
        public override bool CanScale => true;
        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Magic) += ApplyScaling(0.2f);
        }
    }
    public class PermanentFeatherfallPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_295";
        public override int PermanentID => 7;
        public override int BuffType => BuffID.Featherfall;
        public override void PotionEffect(Player player)
        {
            player.slowFall = true;
        }
    }
    public class PermanentSpelunkerPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_296";
        public override int PermanentID => 8;
        public override int BuffType => BuffID.Spelunker;
        public override void PotionEffect(Player player)
        {
            player.findTreasure = true;
        }
    }
    public class PermanentInvisibilityPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_297";
        public override int PermanentID => 9;
        public override int BuffType => BuffID.Invisibility;

        public override void PotionEffect(Player player)
        {
            player.invis = true;
        }
    }
    public class PermanentShinePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_298";
        public override int PermanentID => 10;
        public override int BuffType => BuffID.Shine;

        public override void UpdateInventory(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[10] && !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
            }
        }

        public override void PotionEffect(Player player)
        {
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1)
                {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "", "Has no effect on the [c/6d8827:Bearer of the Curse]"));
                }
            }
        }
    }
    public class PermanentNightOwlPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_299";
        public override int PermanentID => 11;
        public override int BuffType => BuffID.NightOwl;

        public override void PotionEffect(Player player)
        {
            player.nightVision = true;
        }
    }
    public class PermanentBattlePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_300";
        public override int PermanentID => 12;
        public override int BuffType => BuffID.Battle;

        public override void PotionEffect(Player player)
        {
            player.enemySpawns = true;
        }
    }
    public class PermanentThornsPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_301";
        public override int PermanentID => 13;
        public override int BuffType => BuffID.Thorns;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.thorns += ApplyScaling(1f);
        }
    }

    public class PermanentWaterWalkingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_302";
        public override int PermanentID => 14;
        public override int BuffType => BuffID.WaterWalking;

        public override void PotionEffect(Player player)
        {
            player.waterWalk = true;
        }
    }

    public class PermanentArcheryPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_303";
        public override int PermanentID => 15;
        public override int BuffType => BuffID.Archery;

        public override void PotionEffect(Player player)
        {
            player.archery = true;
        }
    }
    public class PermanentHunterPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_304";
        public override int PermanentID => 16;
        public override int BuffType => BuffID.Hunter;

        public override void PotionEffect(Player player)
        {
            player.detectCreature = true;
        }
    }
    public class PermanentGravitationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_305";
        public override int PermanentID => 17;
        public override int BuffType => BuffID.Gravitation;

        public override void PotionEffect(Player player)
        {
            player.gravControl = true;
        }
    }
    public class PermanentAle : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/Ale";
        public override int PermanentID => 18;
        public override int BuffType => BuffID.Tipsy;
        public override bool CanScale => true;
        public override int ScalingFactor => 187;
        public override float EffectPotency {
            get {
                //higher base, slower scaling
                //because having more stats to scale means a low base value hurts a lot more
                //still capped at 150%
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.7f;
                return Math.Min(potency, 1.5f);
            }
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense -= 4; //:smiling_imp:
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetCritChance(DamageClass.Melee) += 2;
            player.GetAttackSpeed(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfVenom : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1340";
        public override int PermanentID => 19;
        public override int BuffType => BuffID.WeaponImbueVenom;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 1;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }
    public class PermanentFlaskOfCursedFlames : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1353";
        public override int PermanentID => 20;
        public override int BuffType => BuffID.WeaponImbueCursedFlames;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 2;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfFire : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1354";
        public override int PermanentID => 21;
        public override int BuffType => BuffID.WeaponImbueFire;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 3;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfGold : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1355";
        public override int PermanentID => 22;
        public override int BuffType => BuffID.WeaponImbueGold;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 4;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfIchor : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1356";
        public override int PermanentID => 23;
        public override int BuffType => BuffID.WeaponImbueIchor;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 5;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfNanites : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1357";
        public override int PermanentID => 24;
        public override int BuffType => BuffID.WeaponImbueNanites;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 6;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfParty : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1358";
        public override int PermanentID => 25;
        public override int BuffType => BuffID.WeaponImbueConfetti;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 7;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFlaskOfPoison : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_1359";
        public override int PermanentID => 26;
        public override int BuffType => BuffID.WeaponImbuePoison;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetFlasks;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.meleeEnchant = 8;
            player.GetDamage(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.SummonMeleeSpeed) += ApplyScaling(0.1f);
        }
    }

    public class PermanentMiningPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2322";
        public override int PermanentID => 27;
        public override int BuffType => BuffID.Mining;

        public override void PotionEffect(Player player)
        {
            player.pickSpeed -= 0.25f;
        }
    }

    public class PermanentHeartreachPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2323";
        public override int PermanentID => 28;
        public override int BuffType => BuffID.Heartreach;

        public override void PotionEffect(Player player)
        {
            player.lifeMagnet = true;
        }
    }

    public class PermanentCalmingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2324";
        public override int PermanentID => 29;
        public override int BuffType => BuffID.Calm;

        public override void PotionEffect(Player player)
        {
            player.calmed = true;
        }
    }
    public class PermanentBuilderPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2325";
        public override int PermanentID => 30;
        public override int BuffType => BuffID.Builder;

        public override void PotionEffect(Player player)
        {
            //who is legitimately crafting this?
            //this doesnt need scaling. fight me.
            player.tileSpeed += 0.25f;
            player.wallSpeed += 0.25f;
            player.blockRange++;
        }
    }
    public class PermanentTitanPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2326";
        public override int PermanentID => 31;
        public override int BuffType => BuffID.Titan;

        public override void PotionEffect(Player player)
        {
            player.kbBuff = true;
        }
    }

    public class PermanentFlipperPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2327";
        public override int PermanentID => 32;
        public override int BuffType => BuffID.Flipper;

        public override void PotionEffect(Player player)
        {
            player.accFlipper = true;
            player.ignoreWater = true;
        }
    }

    public class PermanentSummoningPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2328";
        public override int PermanentID => 33;
        public override int BuffType => BuffID.Summoning;

        public override void PotionEffect(Player player)
        {
            player.maxMinions++;
        }
    }
    public class PermanentDangersensePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2329";
        public override int PermanentID => 34;
        public override int BuffType => BuffID.Dangersense;

        public override void PotionEffect(Player player)
        {
            player.dangerSense = true;
        }
    }

    public class PermanentAmmoReservationPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2344";
        public override int PermanentID => 35;
        public override int BuffType => BuffID.AmmoReservation;

        public override void PotionEffect(Player player)
        {
            player.ammoPotion = true;
        }
    }

    public class PermanentLifeforcePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2345";
        public override int PermanentID => 36;
        public override int BuffType => BuffID.Lifeforce;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.lifeForce = true;
            player.statLifeMax2 += player.statLifeMax / 5 / 20 * (int)(ApplyScaling(20));
        }
    }

    public class PermanentEndurancePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2346";
        public override int PermanentID => 37;
        public override int BuffType => BuffID.Endurance;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.endurance += ApplyScaling(0.1f);
        }
    }

    public class PermanentRagePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2347";
        public override int PermanentID => 38;
        public override int BuffType => BuffID.Rage;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += ApplyScaling(10);
            player.GetCritChance(DamageClass.Melee) += ApplyScaling(10);
            player.GetCritChance(DamageClass.Ranged) += ApplyScaling(10);
            player.GetCritChance(DamageClass.Throwing) += ApplyScaling(10);
        }
    }

    public class PermanentInfernoPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2348";
        public override int PermanentID => 39;
        public override int BuffType => BuffID.Inferno;

        public override void PotionEffect(Player player)
        {
            player.inferno = true;
            Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.65f, 0.4f, 0.1f);
            int num = 24;
            float num12 = 200f;
            bool flag = player.infernoCounter % 60 == 0;
            int damage = 10;
            if (player.whoAmI == Main.myPlayer)
            {
                for (int l = 0; l < 200; l++)
                {
                    NPC nPC = Main.npc[l];
                    if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(player.Center, nPC.Center) <= num12)
                    {
                        if (nPC.FindBuffIndex(num) == -1)
                        {
                            nPC.AddBuff(num, 120);
                        }
                        if (flag)
                        {
                            player.ApplyDamageToNPC(nPC, damage, 0f, 0, crit: false);
                        }
                    }
                }
                if (Main.netMode != NetmodeID.SinglePlayer && player.hostile)
                {
                    for (int m = 0; m < 255; m++)
                    {
                        Player otherPlayer = Main.player[m];
                        if (otherPlayer != player && otherPlayer.active && !otherPlayer.dead && otherPlayer.hostile && !otherPlayer.buffImmune[24] && (otherPlayer.team != player.team || otherPlayer.team == 0) && Vector2.DistanceSquared(player.Center, otherPlayer.Center) <= num)
                        {
                            if (otherPlayer.FindBuffIndex(num) == -1)
                            {
                                otherPlayer.AddBuff(num, 120);
                            }
                            if (flag)
                            {
                                otherPlayer.Hurt(PlayerDeathReason.LegacyEmpty(), damage, 0, pvp: true);
                                if (Main.netMode != NetmodeID.SinglePlayer)
                                {
                                    PlayerDeathReason reason = PlayerDeathReason.ByPlayer(otherPlayer.whoAmI);
                                    NetMessage.SendPlayerHurt(m, reason, damage, 0, critical: false, pvp: true, 0);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class PermanentWrathPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2349";
        public override int PermanentID => 40;
        public override int BuffType => BuffID.Wrath;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Generic) += ApplyScaling(0.1f);
        }
    }

    public class PermanentFishingPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2354";
        public override int PermanentID => 41;
        public override int BuffType => BuffID.Fishing;

        public override void PotionEffect(Player player)
        {
            //people do need need to be punished for making a fishing potion
            player.fishingSkill += 15;
        }
    }

    public class PermanentSonarPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2355";
        public override int PermanentID => 41;
        public override int BuffType => BuffID.Sonar;

        public override void PotionEffect(Player player)
        {
            player.sonarPotion = true;
        }
    }

    public class PermanentCratePotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2356";
        public override int PermanentID => 43;
        public override int BuffType => BuffID.Crate;

        public override void PotionEffect(Player player)
        {
            player.cratePotion = true;
        }
    }

    public class PermanentWarmthPotion : PermanentPotion
    {
        public override string Texture => "Terraria/Images/Item_2359";
        public override int PermanentID => 44;
        public override int BuffType => BuffID.Warmth;

        public override void PotionEffect(Player player)
        {
            player.resistCold = true;
        }
    }

    public class PermanentArmorDrug : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/ArmorDrugPotion";
        public override int PermanentID => 45;
        public override int BuffType => ModContent.BuffType<ArmorDrug>();
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetCombat;
        public override bool CanScale => true;
        public override int ScalingFactor => 180;
        public override float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.67f;
                return Math.Min(potency, 1.5f);
            }
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense += (int)ApplyScaling(25);
            player.endurance += ApplyScaling(0.15f);
        }
    }

    public class PermanentBattlefrontPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/BattlefrontPotion";
        public override int PermanentID => 46;
        public override int BuffType => ModContent.BuffType<Battlefront>();
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetCombat;
        public override bool CanScale => true;
        public override int ScalingFactor => 180;
        public override float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.67f;
                return Math.Min(potency, 1.5f);
            }
        }
        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Generic) += ApplyScaling(0.15f);
            player.GetCritChance(DamageClass.Generic) += ApplyScaling(15);
            player.thorns += ApplyScaling(2f);
            player.enemySpawns = true;
        }
    }

    public class PermanentBoostPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/BoostPotion";
        public override int PermanentID => 47;
        public override int BuffType => ModContent.BuffType<Boost>();
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.moveSpeed *= (1.0f + ApplyScaling(0.2f));
        }
    }

    public class PermanentCrimsonPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/CrimsonPotion";
        public override int PermanentID => 48;
        public override int BuffType => ModContent.BuffType<CrimsonDrain>();

        public override void PotionEffect(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
        }
    }

    public class PermanentDemonDrug : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/DemonDrugPotion";
        public override int PermanentID => 49;
        public override int BuffType => ModContent.BuffType<DemonDrug>();

        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetCombat;
        public override bool CanScale => true;

        public override void PotionEffect(Player player)
        {
            player.GetDamage(DamageClass.Generic) *= ApplyScaling(1.22f);
            player.statDefense -= 15;
        }
    }

    public class PermanentShockwavePotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/ShockwavePotion";
        public override int PermanentID => 50;
        public override int BuffType => ModContent.BuffType<Shockwave>();

        public override void PotionEffect(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
        }
    }

    public class PermanentStrengthPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/StrengthPotion";
        public override int PermanentID => 51;
        public override int BuffType => ModContent.BuffType<Strength>();
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetCombat;
        public override bool CanScale => true;
        public override int ScalingFactor => 180;
        public override float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.67f;
                return Math.Min(potency, 1.5f);
            }
        }

        public override void PotionEffect(Player player)
        {
            player.statDefense += (int)ApplyScaling(15);
            player.GetDamage(DamageClass.Generic) += ApplyScaling(0.1f);
            player.GetAttackSpeed(DamageClass.Generic) += ApplyScaling(0.15f);
        }
    }

    public class PermanentSoulSiphonPotion : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/SoulSiphonPotion";
        public override int PermanentID => 52;
        public override int BuffType => ModContent.BuffType<SoulSiphon>();
        public override bool CanScale => true;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemIconPulse[Item.type] = true; // Makes item pulsate in world.
        }

        public override void PotionEffect(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SoulSiphon = true;
            modPlayer.SoulReaper += 5; //scaling the range would probably just feel bad
            modPlayer.ConsSoulChanceMult += (int)ApplyScaling(10);
        }
    }

    public class PermanentSoup : PermanentPotion
    {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/BowlOfSoup";
        public override int PermanentID => 53;
        public override int BuffType => BuffID.WellFed2;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetWellFed;
        public override bool CanScale => true;
        public override int ScalingFactor => 240; 
        public override float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor; 
                potency += 0.85f; //with how long these things last, decent base values is basically required
                //theres probably some math about "break points" and "decimal rounding" that makes these numbers bad, but whatever, it's 6 am, fight me
                return Math.Min(potency, 1.25f);
            }
        }

        public override void PotionEffect(Player player)
        {
            player.wellFed = true;
            player.statDefense += 3;
            player.GetCritChance(DamageClass.Generic) += ApplyScaling(3);
            player.GetAttackSpeed(DamageClass.Melee) += ApplyScaling(0.075f);
            player.GetDamage(DamageClass.Generic) += ApplyScaling(0.075f);
            player.GetKnockback(DamageClass.Summon) += ApplyScaling(0.75f);
            player.moveSpeed += ApplyScaling(0.30f);
            player.pickSpeed += ApplyScaling(0.1f);
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += ApplyScaling(0.2f);
        }
    }
    public class PermanentTea : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/Teacup";
        public override int PermanentID => 54;
        public override int BuffType => BuffID.WellFed;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetWellFed;
        public override bool CanScale => true;
        public override int ScalingFactor => 240;
        public override float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.85f;
                return Math.Min(potency, 1.25f);
            }
        }

        public override void PotionEffect(Player player) {
            player.wellFed = true;
            player.statDefense += 2;
            player.GetCritChance(DamageClass.Generic) += ApplyScaling(2);
            player.GetAttackSpeed(DamageClass.Melee) += ApplyScaling(0.05f);
            player.GetDamage(DamageClass.Generic) += ApplyScaling(0.05f);
            player.GetKnockback(DamageClass.Summon) += ApplyScaling(0.5f);
            player.moveSpeed += ApplyScaling(0.2f);
            player.pickSpeed -= ApplyScaling(0.05f);
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += ApplyScaling(0.1f);
        }
    }


    public class PermanentGoldenDelight : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/VanillaTextures/GoldenDelight";
        public override int PermanentID => 55;
        public override int BuffType => BuffID.WellFed;
        public override List<PermanentPotion> ExclusivePermanents => ExclusiveSetWellFed;
        public override bool CanScale => true;
        public override int ScalingFactor => 240;
        public override float EffectPotency {
            get {
                float potency = (float)ConsumedAmount / (float)ScalingFactor;
                potency += 0.85f;
                return Math.Min(potency, 1.25f);
            }
        }

        public override void PotionEffect(Player player) {
            player.wellFed = true;
            player.statDefense += 4;
            player.GetCritChance(DamageClass.Generic) += ApplyScaling(4);
            player.GetAttackSpeed(DamageClass.Melee) += ApplyScaling(0.1f);
            player.GetDamage(DamageClass.Generic) += ApplyScaling(0.1f);
            player.GetKnockback(DamageClass.Summon) += ApplyScaling(1f);
            player.moveSpeed += ApplyScaling(0.4f);
            player.pickSpeed -= ApplyScaling(0.15f);
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += ApplyScaling(0.3f);
        }
    }

}
