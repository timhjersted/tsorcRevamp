using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class DragoonArmor2 : ModItem
    {
        public static float DragoonCloakEfficiency = 60f;
        public static float Dmg = 38f;
        public static float MeleeSpeed = 38f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DragoonCloakEfficiency, DragoonCloak.LifeThreshold, Dmg, MeleeSpeed);
        public override string Texture => "tsorcRevamp/Items/Armors/DragoonArmor";
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 50;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
            player.starCloakItem = new Item(ItemID.StarCloak);
            player.lifeRegen += (int)(LightCloak.LifeRegen1 * DragoonCloakEfficiency / 100f);
            player.GetCritChance(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease1 * DragoonCloakEfficiency / 100f;
            player.GetDamage(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease1 * DragoonCloakEfficiency / 100f / 100f;

            player.GetModPlayer<tsorcRevampPlayer>().DarkmoonCloak = true;
            if (player.statLife <= (player.statLifeMax2 * DragoonCloak.LifeThreshold / 100f))
            {
                player.lifeRegen += (int)(LightCloak.LifeRegen2 * DragoonCloakEfficiency / 100f);
                player.statDefense += (int)(DarkCloak.Defense2 * DragoonCloakEfficiency / 100f);
                player.manaRegenBonus += (int)(DarkmoonCloak.ManaRegenBonus * DragoonCloakEfficiency / 100f);
                player.GetCritChance(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease2 * DragoonCloakEfficiency / 100f;
                player.GetDamage(DamageClass.Generic) += DarkmoonCloak.DamageAndCritIncrease2 * DragoonCloakEfficiency / 100f / 100f;
                int dust = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 150, Color.White, 0.5f);
                Main.dust[dust].noGravity = true;
            }
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
            recipe.AddIngredient(ModContent.ItemType<DragoonCloak>());
            //recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
