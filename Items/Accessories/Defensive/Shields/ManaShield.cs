using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive.Shields
{
    public class ManaShield : ModItem
    {

        public static float damageResistance = 35f;
        public static int manaCost = 100;
        public static int regenDelay = 17;
        public static float BadDmgMultiplier = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(damageResistance, manaCost, regenDelay, BadDmgMultiplier);
        public override void SetStaticDefaults()
        {
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
            player.GetDamage(DamageClass.Ranged) *= 1f - BadDmgMultiplier / 100f;
            player.GetDamage(DamageClass.Magic) *= 1f - BadDmgMultiplier / 100f;
            player.GetDamage(DamageClass.Summon) *= 1f - BadDmgMultiplier / 100f;
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
                player.endurance += damageResistance / 100f;
            }
        }
    }
}
