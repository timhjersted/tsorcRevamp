using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class VirulentCatalyzer : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Virulent Catalyzer");
            Tooltip.SetDefault("An enhanced projectile propulsion core allows detonating shots to pierce once"
                                + "\nExtremely toxic - handle with care");
        }

        public override void SetDefaults()
        {
            item.damage = 22;
            item.ranged = true;
            item.crit = 0;
            item.width = 40;
            item.height = 28;
            item.useTime = 17;
            item.useAnimation = 17;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 3f;
            item.value = 350000;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Orange;
            item.shoot = mod.ProjectileType("VirulentCatShot");
            item.shootSpeed = 7f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-5, 4);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }



        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                item.useTime = 26;
                item.useAnimation = 26;
                item.shootSpeed = 10f;
                item.shoot = ModContent.ProjectileType<Projectiles.VirulentCatDetonator>();
            }
            else
            {
                item.useTime = 17;
                item.useAnimation = 17;
                item.shootSpeed = 7f;
                item.shoot = ModContent.ProjectileType<Projectiles.VirulentCatShot>();
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarShot").WithVolume(.6f).WithPitchVariance(.3f), player.Center);
            }

            {
                Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 1f;
                if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
                {
                    position += muzzleOffset;
                    position.Y += 3;
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ToxicCatalyzer"));
            recipe.AddIngredient(ItemID.SpiderFang, 20);
            recipe.AddIngredient(ItemID.HallowedBar, 8);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
