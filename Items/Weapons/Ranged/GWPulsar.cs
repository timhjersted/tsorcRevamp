using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class GWPulsar : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gigawatt Pulsar");
            //Tooltip.SetDefault("");
        }

        public override void SetDefaults()
        {
            item.damage = 69;
            item.ranged = true;
            item.crit = 0;
            item.width = 40;
            item.height = 28;
            item.useTime = 25;
            item.useAnimation = 25;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 3.5f;
            item.value = 40000;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Orange;
            item.shoot = mod.ProjectileType("GWPulsarShot");
            item.shootSpeed = 5.2f;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Pulsar"));
            recipe.AddIngredient(ItemID.HallowedBar, 8);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-5, 4);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarShot").WithVolume(.6f).WithPitchVariance(.3f), player.Center);
            }

            if (player.wet)
            {
                player.AddBuff(BuffID.Electrified, 90);
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
    }
}
