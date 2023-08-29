using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips;

	public class NightsCracker : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			DisplayName.SetDefault("Night's Cracker");
			Tooltip.SetDefault("Stats of this whip scale with how long you've charged it" +
            "\n2-8 summon tag damage" +
            "\nInherits Searing Lash's effect at up to half of its strength" +
            "\n1-4% summon tag critical strike chance" +
            "\nGain 6-24% summon attack speed upon striking an enemy" +
            "\nPerforms better against multiple targets than most whips" +
				"\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 39;
			Item.width = 46;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 42;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(0, 30, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.NightsCrackerProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 30;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.channel = true;

		}
		public override bool MeleePrefix()
		{
			return true;
		}
		public override void AddRecipes()
		{
			
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<Dominatrix>());
			recipe.AddIngredient(ItemID.ThornWhip);
			recipe.AddIngredient(ItemID.BoneWhip);
			recipe.AddIngredient(ModContent.ItemType<SearingLash>());
			recipe.AddIngredient(ItemID.SoulofNight, 20);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 13000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}