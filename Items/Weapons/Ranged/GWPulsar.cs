using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class GWPulsar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gigawatt Pulsar");
            //Tooltip.SetDefault("");
        }

        public override void SetDefaults()
        {
            item.damage = 74;
            item.ranged = true;
            item.crit = 0;
            item.width = 40;
            item.height = 28;
            item.useTime = 25;
            item.useAnimation = 25;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 3.5f;
            item.value = PriceByRarity.Pink_5;
            item.scale = 0.8f;
            item.rare = ItemRarityID.Pink;
            item.shoot = mod.ProjectileType("GWPulsarShot");
            item.shootSpeed = 6.2f;
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

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(item.Right, 0.0452f, 0.24f, 0.24f);

            if (Main.rand.Next(15) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 34, item.position.Y), 8, 28, 226, 1f, 0, 100, default(Color), .4f)];
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = .4f;
            }

            if (Main.rand.Next(10) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X + 34, item.position.Y), 8, 28, 226, 0, 0, 100, default(Color), .4f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += item.velocity;
                dust.fadeIn = .4f;
            }

            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GWPulsarGlowmask];
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
