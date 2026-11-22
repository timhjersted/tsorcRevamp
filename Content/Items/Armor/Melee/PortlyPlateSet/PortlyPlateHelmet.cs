using System.Collections.Generic;
using Humanizer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee.PortlyPlateSet
{
    [AutoloadEquip(EquipType.Head)]
    public class PortlyPlateHelmet : ModItem
    {
        public const float CritChance = 5;
        public const float LifeRegen1 = 2f;
        public const float LifeThreshold = 25f;
        public const float LifeRegen2 = 3f;
        public const int BaseDamage = 45;
        public const float Resistance = 6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<PortlyPlateChestplate>() && legs.type == ModContent.ItemType<PortlyPlateGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus =
                Language.GetTextValue(Tooltip.Key + "SetBonus", LifeRegen1 / 2f, LifeThreshold, LifeRegen2 / 2f,
                    (int)Main.LocalPlayer.GetTotalDamage(DamageClass.Melee).ApplyTo(BaseDamage), Main.LocalPlayer.GetTotalCritChance(DamageClass.Melee),
                    Resistance);
            player.endurance += Resistance / 100f;
            
            player.GetModPlayer<PortlyPlateSetPlayer>().Equipped = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GladiatorHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
