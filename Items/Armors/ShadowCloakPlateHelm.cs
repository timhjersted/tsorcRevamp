using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class ShadowCloakPlateHelm : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Cloak Helm");
            Tooltip.SetDefault("6% increased melee critical strike");
        }
        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 7;
            item.value = 15000;
            item.rare = ItemRarityID.Pink;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShadowCloakPlateMail>() && legs.type == ModContent.ItemType<ShadowCloakGreaves>();
        }

        public override void UpdateEquip(Player player)
        {
            player.meleeCrit += 6;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.meleeDamage += 0.10f;
            player.meleeSpeed += 0.27f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ShadowHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
