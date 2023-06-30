using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Tools
{
    class TomeOfHealth : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.height = 38;
            Item.useTurn = true;
            Item.rare = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 28;
            Item.mana = 240;
        }

        public override bool CanUseItem(Player player)
        {
            return (!player.HasBuff(BuffID.PotionSickness) && !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse);
        }
        public override bool? UseItem(Player player)
        {
            player.statLife += 220;
            if (player.statLife > player.statLifeMax2) player.statLife = player.statLifeMax2;
            player.HealEffect(220, true);
            player.AddBuff(BuffID.PotionSickness, 3600);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer)
            {
                tooltips.Add(new TooltipLine(Mod, "BOTCNoHeal", LangUtils.GetTextValue("CommonItemTooltip.BotCNoHeal")));
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome);
            recipe.AddIngredient(ItemID.LifeCrystal, 10);
            //recipe.AddIngredient(ItemID.CrystalShard, 30);
            recipe.AddIngredient(ItemID.SoulofFlight, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
