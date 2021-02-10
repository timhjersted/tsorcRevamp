using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons
{
    public class Longinus : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Legendary spear fabled to hold sway over the world.\nDoubles attack damage when falling.\nAlso has random chance to cast a healing spell on each strike.");
        }


        public override void SetDefaults()
        {
            item.damage = 200;
            item.knockBack = 9f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 11;
            item.useTime = 1;
            item.shootSpeed = 8;
            //item.shoot = ProjectileID.DarkLance;

            item.height = 50;
            item.width = 50;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 1880000;
            item.rare = ItemRarityID.LightPurple;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.Longinus>();

        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat)
        {
            if (player.velocity.Y > 0)
            {
                mult = 2;
            }

        }

        public override void AddRecipes()
        {
            //incomplete recipe
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Gungnir, 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 3);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 160000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
