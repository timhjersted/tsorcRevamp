using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class GreatSoulArrowStaff : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/SoulArrowStaff";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Soul Arrow");
            Tooltip.SetDefault("Shoots a lightly homing great soul arrow" +
                                "\nwhich can leave enemies Soulstruck" +
                                "\nSoulstruck enemies drop 10% more souls");
        }
        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.width = 40;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 36;
            Item.useTime = 36;
            Item.damage = 38;
            Item.knockBack = 5.5f;
            Item.mana = 9;
            Item.UseSound = SoundID.Item8;
            Item.shootSpeed = 7.5f;
            Item.noMelee = true;
            Item.value = 15000;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.GreatSoulArrow>();
            Item.rare = ItemRarityID.Green;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, 0);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = Mod.GetTexture("Items/Weapons/Magic/GreatSoulArrow_Scroll");
            spriteBatch.Draw(texture, position, new Rectangle(0, 0, texture.Width, texture.Height), drawColor, 0f, origin, scale, SpriteEffects.None, 0.1f);

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = Mod.GetTexture("Items/Weapons/Magic/GreatSoulArrow_Scroll");
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), lightColor, 0f, new Vector2(19, 20), Item.scale, SpriteEffects.None, 0.1f);

            return false;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack) {

            Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 25f;
            if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0)) {
                position += muzzleOffset;
                position.Y += -14;
            }
            float mySpeedX = Main.mouseX + Main.screenPosition.X - position.X;
            float mySpeedY = Main.mouseY + Main.screenPosition.Y - position.Y;
            float speedAbs = (float)Math.Sqrt((mySpeedX * mySpeedX) + (mySpeedY * mySpeedY));
            speedAbs = 7f / speedAbs; // for speed consistency
            mySpeedX *= speedAbs;
            mySpeedY *= speedAbs;
            Projectile.NewProjectile(new Vector2(position.X, position.Y), new Vector2(mySpeedX, mySpeedY), ModContent.ProjectileType<Projectiles.GreatSoulArrow>(), damage, knockBack, player.whoAmI);

            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("SoulArrowStaff").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulSiphonPotion").Type);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
