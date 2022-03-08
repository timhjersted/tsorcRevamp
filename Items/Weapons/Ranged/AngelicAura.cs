using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class AngelicAura : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Full-auto submachine gun with high RoF\n50% chance to not consume ammo");
        }
        public override void SetDefaults() {
            item.ranged = true;
            item.noMelee = true;
            item.useTime = item.useAnimation = 2; //brrrrrr
            item.damage = 38;
            item.knockBack = 1;
            item.autoReuse = true;
            item.shootSpeed = 16;
            item.useAmmo = AmmoID.Bullet;
            item.rare = ItemRarityID.Cyan;
            item.value = PriceByRarity.Cyan_9;
            item.shoot = 10;
            item.height = 50;
            item.width = 32;
            item.useStyle = ItemUseStyleID.HoldingOut;
            
        }

        public override bool ConsumeAmmo(Player player) {
            if (Main.rand.NextFloat(0, 1) < .5) { return false; }
            return base.ConsumeAmmo(player);
        }

        public override Vector2? HoldoutOffset() {
            return new Vector2(-16f, 0f);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            Vector2 offset = new Vector2(speedX, speedY).RotatedByRandom(MathHelper.ToRadians(6));
            speedX = offset.X;
            speedY = offset.Y;
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.VenusMagnum);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
