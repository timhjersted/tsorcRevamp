using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips;

	public class EnchantedWhip : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        DisplayName.SetDefault("Enchanted Whip");
        Tooltip.SetDefault("4 summon tag damage" +
            "\nStrike enemies with an enchantment" +
            "\nEnchanted enemies will be showered by stars upon minion hits" +
            "\nStar damage scales with half of this whips damage" +
            "\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 60;
			Item.width = 52;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 18;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(0, 3, 50, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.EnchantedWhipProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 40; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 40;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}
    public override bool MeleePrefix()
    {
        return true;
    }
    public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.BlandWhip, 1);
			recipe.AddIngredient(ItemID.FallenStar, 10);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
