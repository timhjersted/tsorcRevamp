using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WaterSpiritTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons frost spirits that can pass through walls." +
                                "\nDamage grows as the spirits grow larger");
        }

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 6;
            Item.useTime = 6;
            Item.damage = 40;
            Item.knockBack = 6;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 9;
            Item.mana = 4;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.IceSpirit4>();
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            Vector2 projVel = new Vector2(speedX, speedY);
            projVel = projVel.RotatedBy(MathHelper.ToRadians(Main.rand.Next(-7, 7)));
            speedX = projVel.X;
            speedY = projVel.Y;
            return true;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.CrystalShard, 100);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 40);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
