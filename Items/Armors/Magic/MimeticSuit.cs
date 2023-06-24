using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class MimeticSuit : ModItem
    {
        public static float Dmg = 11f;
        public static int ManaRegen = 7;
        public static float LifeThreshold = 40f;
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
            if (player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f))
            {
                player.manaRegenBonus += ManaRegen;
                player.manaCost -= MimeticHat.ManaCost / 100f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.JungleShirt, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
