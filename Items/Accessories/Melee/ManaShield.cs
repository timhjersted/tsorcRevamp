using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Melee
{
    public class ManaShield : ModItem
    {

        public static int manaCost = 75;
        public static int regenDelay = 1000;
        public static float damageResistance = 0.35f;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Focuses the user's mana into a protective shield" +
                                $"\nReduces incoming damage by {damageResistance * 100}%, but drains {manaCost} mana per hit" +
                                "\nInhibits both natural and artificial mana regen" +
                                $"\n[c/ffbf00:Useful for those who do not specialize in magic]");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Pink_5;
            Item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 1);
            recipe.AddTile(TileID.DemonAltar);
           //recipe.AddCondition(tsorcRevampWorld.SHM1Downed);
            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            //Iterate through the five main accessory slots
            for (int i = 3; i < (8 + player.extraAccessorySlots); i++)
            {
                //If they're wearing the accesories that totally break this concept, it won't function for them.
                if (player.armor[i].type == ItemID.MagicCuffs || player.armor[i].type == ItemID.CelestialCuffs || player.armor[i].type == ItemID.ManaRegenerationBand)
                {
                    player.GetModPlayer<tsorcRevampPlayer>().manaShield = 0;
                    return;
                }
            }

            base.UpdateEquip(player);
            player.GetModPlayer<tsorcRevampPlayer>().manaShield = 1;
            //player.GetDamage(DamageClass.Ranged) *= 0.02f;
            //player.GetDamage(DamageClass.Magic) *= 0.08f;
            //player.GetDamage(DamageClass.Summon) *= 0.02f;
            if (player.statMana >= manaCost)
            {
                player.endurance += damageResistance;
            }
        }
    }
}
