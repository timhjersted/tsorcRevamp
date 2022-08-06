using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class PhazonRifle : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Three round burst \nOnly the first shot consumes ammo\nPhazon rounds are extremely volatile");
        }
        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 12;
            Item.useTime = 4;
            Item.maxStack = 1;
            Item.damage = 25;
            Item.knockBack = 0f;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item31;
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 7;
            Item.useAmmo = 14;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.DamageType = DamageClass.Ranged;
            Item.reuseDelay = 11;
            Item.useAmmo = AmmoID.Bullet;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            type = ModContent.ProjectileType<Projectiles.PhazonRound>();
            return true;
        }


        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return !(player.itemAnimation < Item.useAnimation - 2); //consume 1 ammo instead of 3
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 0);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ClockworkAssaultRifle);
            recipe.AddIngredient(ItemID.MeteoriteBar, 30);
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
