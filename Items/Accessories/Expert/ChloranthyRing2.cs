using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Chloranthy Ring II");
            Tooltip.SetDefault("Increases Stamina recovery speed by 25%" +
                               "\n[c/ffbf00:Further enhances your agility and evasiveness when dodge rolling]" +
                               "\n[c/ffbf00:Grants a 10% chance to evade attacks when hit & superior mid-air dexterity]" +
                               "\nThis ring is named for its decorative green" +
                               "\nblossom. Its luster has now been fully restored." +
                               "\n+4 defense");
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
            Item.expert = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfAttraidies").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<ChloranthyRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.25f;
            player.GetModPlayer<tsorcRevampPlayer>().StaminaReaper = 6;
            player.statDefense += 4;
            player.blackBelt = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is ChloranthyRing)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
