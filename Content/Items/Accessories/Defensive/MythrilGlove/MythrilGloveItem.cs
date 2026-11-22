using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.MythrilGlove
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class MythrilGloveItem : ModItem
    {
        public const int Def1 = 6;
        public const int Def2 = 5;
        public const int Def3 = 7;
        public const float Res1 = 10f;
        public const float Res2 = 8f;
        public const float Res3 = 16f;
        public const float Threshold1 = 66f;
        public const float Threshold2 = 33f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Def1, Res1, Def2 + Def1, Res2 + Res1, Threshold1, Def3 + Def2 + Def1, Res3 + Res2 + Res1, Threshold2);

        public override void SetDefaults()
        {
            Item.width = 30; 
            Item.height = 34; 
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
            Item.accessory = true; 
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<MythrilGlovePlayer>();
            modPlayer.Equipped = true;
            modPlayer.Visual = !hideVisual;
        }
        public override void UpdateVanity(Player player)
        {
            Vector2 value10 = Main.OffsetsPlayerOnhand[player.bodyFrame.Y / 56] * 2f;

            if (player.direction != 1)
            {
                value10.X = (float)player.bodyFrame.Width - value10.X;
            }

            if (player.gravDir != 1f)
            {
                value10.Y = (float)player.bodyFrame.Height - value10.Y;
            }

            value10 -= new Vector2(player.bodyFrame.Width - player.width, player.bodyFrame.Height - 42) / 2f;
            Vector2 position = player.RotatedRelativePoint(player.position + value10) - player.velocity;
            if (Main.rand.NextBool(80))
            {
                for (int num183 = 0; num183 < 2; num183++)
                {
                    Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 57, player.direction * 2, 0f, 150, default(Color), .4f)]; //gold dust
                    dust.position = position;
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                    dust.noLight = true;

                    if (Main.rand.NextBool(2))
                    {
                        dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                        dust.scale += Main.rand.NextFloat();

                        if (Main.rand.NextBool(2))
                        {
                            dust.customData = player;
                        }
                    }
                }
            }
            if (Main.rand.NextBool(80))
            {
                for (int num183 = 0; num183 < 2; num183++)
                {
                    Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, 180, player.direction * 2, 0f, 150, default(Color), .4f)]; //blue dust
                    dust.position = position;
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                    dust.noLight = true;

                    if (Main.rand.NextBool(2))
                    {
                        dust.position += Utils.RandomVector2(Main.rand, -4f, 4f);
                        dust.scale += Main.rand.NextFloat();

                        if (Main.rand.NextBool(2))
                        {
                            dust.customData = player;
                        }
                    }
                }
            }
        }

        

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TitanGlove);
            recipe.AddIngredient(ItemID.MythrilBar, 3);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 50000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
