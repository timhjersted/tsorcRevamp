using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.ShadowCloakSet
{
    [AutoloadEquip(EquipType.Body)]
    public class ShadowCloakPlateMail : ModItem
    {
        public const float Dmg = 9f;
        public const float LifeThreshold = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, LifeThreshold);
        public override void SetStaticDefaults()
        {
        }

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
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<ShadowCloakPlateHelm>() && legs.type == ModContent.ItemType<ShadowCloakGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            var modPlayer = player.GetModPlayer<ShadowCloakSetPlayer>();
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
            recipe.AddIngredient(ItemID.ShadowScalemail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientShadowScalemail);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
