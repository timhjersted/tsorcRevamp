using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class AnkorWatChestplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+25% Magic Critical chance.\nSet bonus: +130 max mana, Rapid Mana Regen");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 25;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<AnkorWatHelmet>() && legs.type == ModContent.ItemType<AnkorWatLeggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.manaRegenBuff = true;
            player.statManaMax2 += 130;
            player.manaRegen += 7;
            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Red, 1.0f);
            Main.dust[dust].noGravity = true;
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedPlateMail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
