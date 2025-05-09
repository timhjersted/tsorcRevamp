using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Accessories.Damage;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.Items.Debug
{
    [AutoloadEquip(EquipType.Body)]

    public class Linklight2sShirt : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/RedHerosShirt";
        public static float HeroCloakEfficiency = 100f;
        public static float Dmg = 50f;
        public static float MeleeSpeed = 50f;
        public static float HeroLifeThreshold = 80f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HeroCloakEfficiency, HeroLifeThreshold, Dmg, MeleeSpeed);

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 45;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            if (!Terraria.ModLoader.ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }

            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
            player.starCloakItem = new Item(ItemID.StarCloak);
            player.lifeRegen += (int)(LightCloak.LifeRegen1 * HeroCloakEfficiency / 100f);
            player.GetCritChance(DamageClass.Generic) += ShadowmoonCloak.DamageAndCritIncrease1 * HeroCloakEfficiency / 100f;
            player.GetDamage(DamageClass.Generic) += ShadowmoonCloak.DamageAndCritIncrease1 * HeroCloakEfficiency / 100f / 100f;

            player.GetModPlayer<tsorcRevampPlayer>().ShadowmoonCloak = true;

            // Ankh Charm Effect
            player.buffImmune[33] = true;
            player.buffImmune[36] = true;
            player.buffImmune[30] = true;
            player.buffImmune[20] = true;
            player.buffImmune[32] = true;
            player.buffImmune[31] = true;
            player.buffImmune[35] = true;
            player.buffImmune[23] = true;
            player.buffImmune[22] = true;
            player.buffImmune[156] = true;

            if (Main.rand.NextBool(25))
            {
                int dust = Dust.NewDust(new Vector2((float)player.position.X - 20, (float)player.position.Y - 20), player.width + 40, player.height + 20, 89, 0, 0, 0, default, .5f);
                Main.dust[dust].velocity *= 0.25f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1f;
            }

            if (player.statLife <= (player.statLifeMax2 * HeroLifeThreshold / 100f))
            {
                player.lifeRegen += (int)(LightCloak.LifeRegen2 * HeroCloakEfficiency / 100f);
                player.statDefense += (int)(DarkCloak.Defense2 * HeroCloakEfficiency / 100f);
                player.manaRegenBonus += (int)(ShadowmoonCloak.ManaRegenBonus * HeroCloakEfficiency / 100f);
                player.GetCritChance(DamageClass.Generic) += ShadowmoonCloak.DamageAndCritIncrease2 * HeroCloakEfficiency / 100f;
                player.GetDamage(DamageClass.Generic) += ShadowmoonCloak.DamageAndCritIncrease2 * HeroCloakEfficiency / 100f / 100f;
                int dust = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 150, Color.White, 0.5f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddCondition(tsorcRevampWorld.DebugModeEnabled);
            recipe.AddIngredient(ItemID.CopperChainmail, 1);

            recipe.Register();
        }
    }
}
