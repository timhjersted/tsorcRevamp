using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MirkwoodElvenBlondeHairStyle : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Gifted with ranged combat. High defense not necessary\nWith eyes of a hunter you can spot enemies in the dark");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 1;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MirkwoodElvenLeatherArmor>() && legs.type == ModContent.ItemType<MirkwoodElvenLeggings>();
        }

        public override void UpdateEquip(Player player)
        {
            player.detectCreature = true;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.2f;
            player.GetCritChance(DamageClass.Ranged) += 20;
            player.lifeRegen += 9;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.MythrilHat, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
