using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class SupremeDragoonLance : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Spears/DragoonLance";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Supreme Dragoon Lance");
            // Tooltip.SetDefault("An all-powerful spear forged from the fang of the Dragoon Serpent.");
        }

        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.knockBack = 15f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 1;
            Item.shootSpeed = 11;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Spears.DragoonLanceProj>();

        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            /*
            RasterizerState OverflowHiddenRasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 5; i++)
            {
                Vector2 offsetPositon = Vector2.UnitX.RotatedBy((i * MathHelper.Pi / 2) + MathHelper.PiOver4);
                offsetPositon *= 2;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.White, 0, origin, scale, SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);
            */
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragoonLance>());
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 9);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 9);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 170000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
