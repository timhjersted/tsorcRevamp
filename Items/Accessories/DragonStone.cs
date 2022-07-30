using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class DragonStone : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Imbues swords with fire, raises damage dealt by 5% and provides immunity to" +
                                "\nmost flying creatures, lava, catching on fire, knockback, and fire blocks.");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        /*public override void AddRecipes() {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofFlight, 70);
            recipe.AddIngredient(Mod.Find<ModItem>("RedTitanite").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
        */

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.05f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.lavaImmune = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.AddBuff(BuffID.WeaponImbueFire, 60, false);
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DragonStone = true;
        }


    }
}
