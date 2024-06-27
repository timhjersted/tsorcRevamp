using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class DragoonHelmet2 : ModItem
    {
        public static int MaxMana = 200;
        public static float ManaCost = 17f;
        public static int ManaRegen = 11;
        public static float CritChance = 38f;
        public static int LifeRegen = 4;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost, ManaRegen, CritChance, LifeRegen);
        public override void SetStaticDefaults()
        {
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
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
            player.manaRegenBonus += ManaRegen;
            player.pStone = true;
            player.lifeRegen += LifeRegen;
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
            player.GetCritChance(DamageClass.Generic) += CritChance;

            //player.wings = 34; // looks like Jim's Wings
            //player.wingsLogic = 34;
            //player.wingTimeMax = 180;
            player.ignoreWater = true;
            player.iceSkate = true;
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
            recipe.AddIngredient(ItemID.CharmofMyths);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 54000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
