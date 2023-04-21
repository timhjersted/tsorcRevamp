using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Melee;
using System.Collections.Generic;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    [Autoload(true)]
    public class OrbOfDeception : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 20;
            Item.mana = 20;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.shootSpeed = 20f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfDeceptionOrb>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().OrbExists)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionFlame>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionOrb>();
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().OrbExists && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief >= 9)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionOrbFilled>();
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowOrb);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
