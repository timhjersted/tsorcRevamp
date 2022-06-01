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
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useTime = Item.useAnimation = 2; //brrrrrr
            Item.damage = 38;
            Item.knockBack = 1;
            Item.autoReuse = true;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
            Item.shoot = 10;
            Item.height = 50;
            Item.width = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) {
            if (Main.rand.NextFloat(0, 1) < .5) { return false; }
            return base.CanConsumeAmmo(ammo, player);
        }

        public override Vector2? HoldoutOffset() {
            return new Vector2(-16f, 0f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(MathHelper.ToRadians(6));
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.VenusMagnum);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
