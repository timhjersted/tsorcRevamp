using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Magic;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class ManaShield : ModItem
    {

        public static int manaCost = 100;
        public static int regenDelay = 1000;
        public static float damageResistance = 0.35f;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Focuses the user's mana into a protective shield" +
                                $"\nReduces incoming damage by {damageResistance * 100}%, but drains {manaCost} mana per hit" +
                                "\nInhibits both natural and artificial mana regen" +
                                $"\n[c/ffbf00:Useful for those who do not specialize in magic]" +
                                $"\nReduces ranged, magic summon damage by 25% multiplicatively");
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
            player.GetDamage(DamageClass.Ranged) *= 0.75f;
            player.GetDamage(DamageClass.Magic) *= 0.75f;
            player.GetDamage(DamageClass.Summon) *= 0.75f;
            //Iterate through the five main accessory slots
            for (int i = 3; i < (8 + player.extraAccessorySlots); i++)
            {
                //If they're wearing the accesories that totally break this concept, it won't function for them.
                if (player.armor[i].type == ItemID.MagicCuffs || player.armor[i].type == ItemID.CelestialCuffs || player.armor[i].type == ItemID.ManaRegenerationBand || player.armor[i].type == ModContent.ItemType<CelestialCloak>())
                {
                    player.GetModPlayer<tsorcRevampPlayer>().manaShield = 0;
                    return;
                }
            }
            player.GetModPlayer<tsorcRevampPlayer>().manaShield = 1;
            if (player.statMana >= manaCost)
            {
                player.endurance += damageResistance;
            }
        }
    }
}
