using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Items
{
    class StaminaVessel : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently increases max stamina by 5%" +
                               "\nStamina maxes out after consuming 10");
        }

        public override void SetDefaults()
        {
            item.width = 30;
            item.height = 42;
            item.rare = ItemRarityID.Expert;
            item.value = 0;
            item.useAnimation = 60;
            item.useTime = 60;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.UseSound = SoundID.Item4;
            item.maxStack = 99;
            item.consumable = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            tooltips.Insert(4, new TooltipLine(mod, "", $"Consumed so far: { (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax - 100) / 5}"));

        }

        public override bool CanUseItem(Player player)
        {
            return (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax < 150);
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax += 5;
            return true;
        }
    }
}
