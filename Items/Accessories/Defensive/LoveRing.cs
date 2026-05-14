using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class LoveRing : ModItem
    {
        public const int HealAmount = 8;
        public const int LifeRegen = 3;
        public const int Cooldown = 120; // 2 seconds
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen, HealAmount);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 34;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.LightPurple_6;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Aggro increase, same as flesh knuckles 
            player.aggro += 400;
            player.lifeRegen += LifeRegen;

            // Increase life regen for all near players 
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player ally = Main.player[i];
                if (ally.active && !ally.dead && ally.team == player.team && ally.team != 0)
                {
                    if (Vector2.Distance(player.Center, ally.Center) < 800f)
                    {
                        ally.lifeRegen += LifeRegen;
                    }
                }
            }

            player.GetModPlayer<tsorcRevampPlayer>().HasLoveRing = true;
        }
    }
}