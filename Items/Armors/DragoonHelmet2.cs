using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class DragoonHelmet2 : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/DragoonHelmet";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Supreme Dragoon Helmet");
            /* Tooltip.SetDefault("+200 Mana and Philosopher's stone effect" +
                "\nSet Bonus: Harmonized with the four elements: fire, water, earth and air, including +5 life regen and flight" +
                "\nBoosts damage, crit chance, melee and movement speed by 38% and reduces mana costs by 17%"); */
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 15;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 200;
            player.pStone = true;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DragoonArmor2>() && legs.type == ModContent.ItemType<DragoonGreaves2>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Generic) += 0.38f;
            player.GetCritChance(DamageClass.Generic) += 38;
            player.GetAttackSpeed(DamageClass.Melee) += 0.38f;
            player.moveSpeed += 0.38f;
            player.manaCost -= 0.17f;
            player.lifeRegen += 5;
            player.GetModPlayer<tsorcRevampPlayer>().DarkmoonCloak = true;

            //player.wings = 34; // looks like Jim's Wings
            //player.wingsLogic = 34;
            player.wingTimeMax = 180;

        }

        public override bool WingUpdate(Player player, bool inUse)
        {
            return base.WingUpdate(player, inUse);
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
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
            recipe.AddIngredient(ModContent.ItemType<DragoonHelmet>());
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
