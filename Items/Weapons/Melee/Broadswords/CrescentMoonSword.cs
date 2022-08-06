using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class CrescentMoonSword : ModItem 
    {                                        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crescent Moon Sword");
            Tooltip.SetDefault("Ringfinger Leonhard's weapon of choice," +
                               "\na type of shotel imbued with the power of the moon" +
                               "\nShoots beams of crescent moonlight that pierce walls at night");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Cyan;
            Item.damage = 61;
            Item.width = 40;
            Item.height = 40;
            Item.knockBack = 4.5f;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1f;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 100000;
            Item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
            Item.shootSpeed = 4.5f; //Projectile has same range as the Ancient Blood Lance
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (!Main.dayTime)
            {
                Item.shoot = ModContent.ProjectileType<Projectiles.CrescentTrue>();
                Item.shootSpeed = 12f;

            }
            else
            {
                Item.shoot = ModContent.ProjectileType<Projectiles.CMSCrescent>();
                Item.shootSpeed = 4.5f; //Projectile has same range as the Ancient Blood Lance
            }

            return true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if ((player.name == "Zeodexic") || (player.name == "ChromaEquinox")) //*/) //Add whatever names you use -C
            {
                Item.damage = 120; //change this to whatever suits your testing needs -C
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Melee.ShatteredMoonlight>(), 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ItemID.SoulofLight, 7);
            recipe.AddIngredient(ItemID.SoulofNight, 7);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
