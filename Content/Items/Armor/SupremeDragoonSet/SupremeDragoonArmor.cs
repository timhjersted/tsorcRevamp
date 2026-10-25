using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive;
using tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;
using tsorcRevamp.Content.Items.Accessories.Other;
using tsorcRevamp.Content.Items.Accessories.Other.DragoonCloak;
using tsorcRevamp.Content.Items.Armor.DragoonSet;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.SupremeDragoonSet
{
    [AutoloadEquip(EquipType.Body)]
    public class SupremeDragoonArmor : ModItem
    {
        public const float DragoonCloakEfficiency = 60f;
        public const float Dmg = 32f;
        public const float MeleeSpeed = 35f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DragoonCloakEfficiency, DragoonCloakItem.LifeThreshold, Dmg, MeleeSpeed);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 28;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            RasterizerState OverflowHiddenRasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.White, 0, origin, scale, SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragoonArmor>());
            recipe.AddIngredient(ModContent.ItemType<DragoonCloakItem>());
            //recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
