using tsorcRevamp.Buffs;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
	public class AbyssFlask : ModItem
	{
        public const float DamageCritIncrease = 6f;
		public override void SetStaticDefaults() 
        {
			ItemID.Sets.DrinkParticleColors[Type] = [
				new Color(240, 240, 240),
				new Color(200, 200, 200),
				new Color(140, 140, 140)
			];
		}

		public override void SetDefaults() {
			Item.UseSound = SoundID.Item3;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useTurn = true;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.width = 14;
			Item.height = 24;
			Item.buffType = ModContent.BuffType<Buffs.AbyssWeaponImbue>();
			Item.buffTime = Item.flaskTime;
			Item.value = Item.sellPrice(0, 0, 5);
			Item.rare = ItemRarityID.Red;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient<Items.Materials.FlameOfTheAbyss>(2)
				.AddTile(TileID.ImbuingStation)
				.Register();
		}
	}
}