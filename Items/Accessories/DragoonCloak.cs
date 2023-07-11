using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class DragoonCloak : ModItem
    {
        public static float LifeThreshold = (LightCloak.LifeThreshold + DarkmoonCloak.LifeThreshold + DarkCloak.LifeThreshold) / 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeThreshold);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 36;
            Item.defense = 7;
            Item.accessory = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<LightCloak>());
            recipe.AddIngredient(ModContent.ItemType<DarkCloak>());
            recipe.AddIngredient(ModContent.ItemType<DarkmoonCloak>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);

            player.GetModPlayer<tsorcRevampPlayer>().DarkmoonCloak = true;
            player.lifeRegen += LightCloak.LifeRegen1;
            player.starCloakItem = new Item(ItemID.StarCloak);
            player.GetCritChance(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease1;
            player.GetDamage(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease1 / 100f;


            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.lifeRegen += LightCloak.LifeRegen2 - LightCloak.LifeRegen1;
                player.statDefense += DarkCloak.Defense2;
                player.manaRegenBonus += DarkmoonCloak.ManaRegenBonus;
                player.GetCritChance(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease2;
                player.GetDamage(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease2 / 100f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
