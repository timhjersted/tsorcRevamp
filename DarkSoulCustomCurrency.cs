using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp
{
	public class DarkSoulCustomCurrency : CustomCurrencySingleCoin
	{

		public DarkSoulCustomCurrency(int coinItemID, long currencyCap) : base(coinItemID, currencyCap)
		{
		}
		public override void GetPriceText(string[] lines, ref int currentLine, int price)
		{
			lines[currentLine++] = string.Format("[c/00FF50:{0} {1} {2}]" + $"[i:{ ModContent.ItemType<SoulShekel>() }]", new object[]
			{
				Language.GetTextValue("LegacyTooltip.50"),
				price,
				"Soul Shekels"
			});
		}
	}
}

