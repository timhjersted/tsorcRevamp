using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Accessories.Melee;

namespace tsorcRevamp.Items.Accessories.Damage
{
    public class DragonSoul : ModItem
    {
        public static int Potency = 5;
        public static float DamageIncrease = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Potency, DamageIncrease);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.expert = true;
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 5;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Yellow * 0.3f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 5;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Yellow * 0.3f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().DragonStoneImmunity = true;
            tsorcRevampPlayer.DragonStonePotency = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragonSoulEffect = true;
            player.GetDamage(DamageClass.Generic) += DamageIncrease / 100f;     
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual) player.inferno = true;
        }


        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragonStone>());
            recipe.AddIngredient(ModContent.ItemType<MoltenRing>());
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>());
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 6);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
