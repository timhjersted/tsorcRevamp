using Terraria.GameContent.UI;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    public class DarkSoulCustomCurrency : CustomCurrencySingleCoin
    {

        public DarkSoulCustomCurrency(int coinItemID, long currencyCap) : base(coinItemID, currencyCap)
        {
        }
        public override void GetPriceText(string[] lines, ref int currentLine, long price)
        {
            lines[currentLine++] = string.Format("[c/00FF50:{0} {1} {2}]" + $"[i:{ModContent.ItemType<SoulCoin>()}]", new object[]
            {
                Language.GetTextValue("LegacyTooltip.50"),
                price,
                LaUtils.GetTextValue("Items.SoulCoin.Plural")
            });
        }
    }
}

