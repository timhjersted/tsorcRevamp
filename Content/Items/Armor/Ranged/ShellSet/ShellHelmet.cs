using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Ranged.ShellSet
{
    [AutoloadEquip(EquipType.Head)]
    class ShellHelmet : ModItem
    {
        public const float CritChance = 16f;
        public const float LifeThreshold = 50f;
        public const int AmmoChance = 20;  //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public const float CritCap = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, LifeThreshold, AmmoChance, CritCap);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 7;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShellArmor>() && legs.type == ModContent.ItemType<ShellGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            var modPlayer = player.GetModPlayer<ShellSetPlayer>();
            modPlayer.Equipped = true;
            bool isVisible = player.armor[10].IsAir && player.armor[11].IsAir && player.armor[12].IsAir;
            if (isVisible)
            {
                modPlayer.Visual = true;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.NecroHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
