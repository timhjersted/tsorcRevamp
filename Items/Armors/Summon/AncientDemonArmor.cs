using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientDemonArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Forged by those who brave Annihilation.\n+9% minion damage\nSet bonus: +20% whip range, +25% whip speed and 15% extra when under 166 life\n+15% whip damage");
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
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
            player.GetDamage(DamageClass.Summon) += 0.09f;
        }

        public override void DrawArmorColor(Player drawPlayer, float shadow, ref Color color, ref int glowMask, ref Color glowMaskColor)
        {
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(new Vector2((float)drawPlayer.position.X, (float)drawPlayer.position.Y), drawPlayer.width, drawPlayer.height, 6, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, Color.Yellow, 1.0f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = false;
            }
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianShirt, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
