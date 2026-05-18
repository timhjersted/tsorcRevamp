using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Tools
{
    class TomeOfHealth : ModItem
    {
        public const int HealAmount = 220;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HealAmount);
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
            // BotC: blocked entirely. Classic + Unkindled: usable (Unkindled at reduced effect).
            return !(player.potionDelay > 0 || player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse);
        }
        public override bool? UseItem(Player player)
        {
            // Tier-aware heal: Classic full HealAmount, Unkindled half, BotC zero (CanUseItem blocks).
            int heal = player.GetModPlayer<tsorcRevampPlayer>().ApplyHealing(HealAmount);
            player.statLife += heal;
            if (player.statLife > player.statLifeMax2) player.statLife = player.statLifeMax2;
            player.HealEffect(heal, true);
            player.AddBuff(BuffID.PotionSickness, ((player.pStone) ? 45 : 60) * 60);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.BearerOfTheCurse)
            {
                tooltips.Add(new TooltipLine(Mod, "BOTCNoHeal", LangUtils.GetTextValue("CommonItemTooltip.BotCNoHeal")));
            }
            else if (modPlayer.Unkindled)
            {
                tooltips.Add(new TooltipLine(Mod, "UnkindledReducedHeal", LangUtils.GetTextValue("CommonItemTooltip.UnkindledReducedHeal")));
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
