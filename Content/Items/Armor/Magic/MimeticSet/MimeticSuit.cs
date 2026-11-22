using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Magic.MimeticSet
{
    [AutoloadEquip(EquipType.Body)]
    public class MimeticSuit : ModItem
    {
        public const float Dmg = 11f;
        public const int ManaRegen = 7;
        public const float LifeThreshold = 40f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, ManaRegen, LifeThreshold);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.manaRegenBonus += ManaRegen;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<MimeticHat>() && legs.type == ModContent.ItemType<MimeticPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            var modPlayer = player.GetModPlayer<MimeticSetPlayer>();
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
            recipe.AddIngredient(ItemID.JungleShirt, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
