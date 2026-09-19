using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings
{
    public class GreyWolfRing : ModItem
    {
        public static int AbyssDef = 12;
        public static float MaxLifeIncrease = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AbyssDef, CrackedDragonStone.DR, RingOfClarity.LifeRegen, MaxLifeIncrease);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 18;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ModContent.RarityType<OrangeRed>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<WolfRing>());
            recipe.AddIngredient(ModContent.ItemType<RingOfClarity>());
            recipe.AddIngredient(ModContent.ItemType<ZirconRing>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.WolfRing = true;
            modPlayer.ZirconRing = true;

            //Ring of Clarity inheritance
            player.lifeRegen += (int)RingOfClarity.LifeRegen;
            player.endurance += CrackedDragonStone.DR / 100f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Chilled] = true;
            player.buffImmune[BuffID.CursedInferno] = true;
            player.buffImmune[BuffID.Frostburn] = true;
            player.buffImmune[BuffID.Ichor] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[BuffID.Frozen] = true;
            player.buffImmune[BuffID.Blackout] = true;
            player.buffImmune[BuffID.Obstructed] = true;
            player.buffImmune[BuffID.Venom] = true;
            player.buffImmune[ModContent.BuffType<Frostbite>()] = true;
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