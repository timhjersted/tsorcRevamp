using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class Celestriad : ModItem
    {
        public static float damageResistance = 35f;
        public static int manaCost = 90;
        public static float MaxManaPercentIncrease = 100f;
        public static float StaminaRegen = 15f;
        public static int regenDelay = 13;
        public static float BadDmgMultiplier = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(damageResistance, manaCost, MaxManaPercentIncrease, StaminaRegen, regenDelay, BadDmgMultiplier);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ManaShield>(), 1);
            recipe.AddIngredient(ModContent.ItemType<EssenceOfMana>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 30);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 200000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegen / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().MaxManaAmplifier += MaxManaPercentIncrease;
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
            player.GetModPlayer<tsorcRevampPlayer>().manaShield = 2;
            if (player.statMana >= manaCost)
            {
                player.endurance += damageResistance / 100f;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, DustID.AncientLight, player.velocity.X, player.velocity.Y, 150, Color.White, 0.5f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
