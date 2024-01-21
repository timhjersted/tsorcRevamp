using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Bands;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories.Defensive.Rings
{
    public class GreyWolfRing : ModItem
    {
        public static int AbyssDef = 12;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AbyssDef, BandOfPhenomenalCosmicPower.LifeRegen, BandOfPhenomenalCosmicPower.MaxManaIncrease, BandOfPhenomenalCosmicPower.ManaRegen, BandOfPhenomenalCosmicPower.ManaRegenDelay);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 19;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<WolfRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BandOfPhenomenalCosmicPower>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RingOfClarity>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().WolfRing = true;

            //Band of Phenomenal Cosmic Power inheritance
            player.statManaMax2 += BandOfPhenomenalCosmicPower.MaxManaIncrease;
            player.lifeRegen += BandOfPhenomenalCosmicPower.LifeRegen;
            player.manaRegenBonus += BandOfPhenomenalCosmicPower.ManaRegen;
            player.manaRegenDelayBonus += BandOfPhenomenalCosmicPower.ManaRegenDelay / 100f;

            //Ring of Clarity inheritance
            player.GetDamage(DamageClass.Generic) += CrackedDragonStone.Dmg / 100f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Chilled] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.Ichor] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[ModContent.BuffType<Frostbite>()] = true;

            //Wolf Ring inheritance
            if (Main.bloodMoon)
            { // Apparently this is the flag used in the Abyss?
                player.statDefense += WolfRing.AbyssDef;
            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual) player.AddBuff(BuffID.WeaponImbueVenom, 1, false);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var WolfRingKey = tsorcRevamp.WolfRing.GetAssignedKeys();
            string WolfRingString = WolfRingKey.Count > 0 ? WolfRingKey[0] : LangUtils.GetTextValue("Keybinds.Wolf Ring.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip2");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.WolfRing.Keybind1") + WolfRingString + Language.GetTextValue("Mods.tsorcRevamp.Items.WolfRing.Keybind2")));
            }
        }
    }
}