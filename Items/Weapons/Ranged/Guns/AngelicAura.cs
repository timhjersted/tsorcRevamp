using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Guns
{
    public class AngelicAura : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Full-auto submachine gun with high RoF\n50% chance to not consume ammo");
        }
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useTime = Item.useAnimation = 3; //brrrrrr
            Item.damage = 50;
            Item.knockBack = 1;
            Item.autoReuse = true;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.shoot = 10;
            Item.height = 50;
            Item.width = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item11;

        }

        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            if (Main.rand.NextFloat(0, 1) < .5) { return false; }
            return base.CanConsumeAmmo(ammo, player);
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-16f, 0f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(MathHelper.ToRadians(10));
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.VenusMagnum);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 8);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
