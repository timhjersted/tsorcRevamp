using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Items
{
    class PurgingStone : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.StoneBlock, 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 2);
            recipe.AddIngredient(ModContent.ItemType<GreenBlossom>(), 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!Main.mouseLeft)
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if ((modPlayer.CurseActive || modPlayer.powerfulCurseActive) && Main.myPlayer == player.whoAmI)
            {
                return true;
            }
            return false;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (Main.myPlayer == player.whoAmI)
            {
                modPlayer.CurseLevel = 0;
                modPlayer.CurseActive = false;
                modPlayer.PowerfulCurseLevel = 0;
                modPlayer.powerfulCurseActive = false;
                Dust.NewDust(player.TopLeft, player.width, player.height, DustID.Blood, Main.rand.NextFloat(2), Main.rand.NextFloat(2), 0, default, 1);
                SoundEngine.PlaySound(SoundID.Item104 with { Volume = 1f});
                return true;
            }
            return false;
        }
    }
}
