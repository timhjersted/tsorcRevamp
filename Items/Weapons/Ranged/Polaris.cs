using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Polaris : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Polaris");
            Tooltip.SetDefault("Batteries included");
        }

        public override void SetDefaults()
        {
            Item.damage = 175;
            Item.DamageType = DamageClass.Ranged;
            Item.crit = 0;
            Item.width = 56;
            Item.height = 28;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = 1000000;
            Item.scale = 0.8f;
            Item.rare = ItemRarityID.Lime;
            Item.autoReuse = true;
            Item.shoot = Mod.Find<ModProjectile>("PolarisShot").Type;
            Item.shootSpeed = 8f;
        }


        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 4);
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarShot").WithVolume(.6f).WithPitchVariance(.3f), player.Center);
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
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Right, 0.0452f, 0.24f, 0.24f);

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 34, Item.position.Y), 8, 28, 226, 1f, 0, 100, default(Color), .4f)];
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = .4f;
            }

            if (Main.rand.Next(5) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X + 34, Item.position.Y), 8, 28, 226, 0, 0, 100, default(Color), .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = .4f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PolarisGlowmask];
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
        public override void AddRecipes()
        {

            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(null, "GWPulsar");
            recipe.AddIngredient(ItemID.LihzahrdPowerCell, 2);
            recipe.AddIngredient(ItemID.ShroomiteBar, 10);
            recipe.AddIngredient(ItemID.ElectrosphereLauncher);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 125000);
            recipe.AddTile(TileID.MythrilAnvil);
            
            recipe.Register();

        }
    }
}
