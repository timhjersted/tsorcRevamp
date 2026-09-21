using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Shields
{
    [AutoloadEquip(EquipType.Shield)]
    public class AncientDemonShield : ModItem
    {
        public static float DamageReduction = 5f;
        public static float Thorns = 1f;
        public static int SoulCost = 4000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageReduction, Thorns * 100);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.defense = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            if (incomingItem.type == ModContent.ItemType<IronShield>() || incomingItem.type == ModContent.ItemType<SpikedIronShield>())
            {
                return false;
            }
            return base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Knockback immunity, fire-walking, and passive resistance stay active in both modes.
            player.noKnockback = true;
            player.fireWalk = true;
            player.endurance += DamageReduction / 100f;

            // Passive thorns is classic-only; active mode replaces it with on-parry reflect.
            if (!tsorcRevampActiveShieldPlayer.ActiveFor(player))
            {
                player.thorns += Thorns;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianShield);
            recipe.AddIngredient(ModContent.ItemType<SpikedIronShield>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
