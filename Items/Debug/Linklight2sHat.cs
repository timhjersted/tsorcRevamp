using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Items.Debug
{
    [AutoloadEquip(EquipType.Head)]
    public class Linklight2sHat : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/RedHerosHat";
        public static int MaxMana = 500;
        public static float ManaCost = 50f;
        public static int ManaRegen = 20;
        public static float CritChance = 50f;
        public static int LifeRegen = 25;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost, ManaRegen, CritChance, LifeRegen);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 25;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            if (!Terraria.ModLoader.ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }

            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
            player.manaRegenBonus += ManaRegen;
            player.pStone = true;
            player.lifeRegen += LifeRegen;
            player.CanSeeInvisibleBlocks = true;

            int cursorX = (int)((Main.mouseX + Main.screenPosition.X) / 16);
            int cursorY = (int)((Main.mouseY + Main.screenPosition.Y) / 16);
            Lighting.AddLight(cursorX, cursorY, 1.5f, 1.5f, 1.5f);
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Linklight2sShirt>() && legs.type == ModContent.ItemType<Linklight2sPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            if (!Terraria.ModLoader.ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetCritChance(DamageClass.Generic) += CritChance;

            player.ignoreWater = true;
            player.iceSkate = true;
            player.endurance += 150f / 100f;
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
            recipe.AddIngredient(ItemID.CopperHelmet, 1);
            recipe.AddCondition(tsorcRevampWorld.DebugModeEnabled);
            recipe.Register();
        }
    }
}