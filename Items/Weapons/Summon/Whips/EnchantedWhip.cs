using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class EnchantedWhip : ModItem
	{

		public override void SetStaticDefaults() //journey mode lmao
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
			Tooltip.SetDefault("10 tag dmg, 10% tag crit chance");
		}

		public override void SetDefaults()
		{

			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 20;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Green;

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.EnchantedWhipProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.UseSound = SoundID.Item152;
			Item.channel = true; // This is used for the charging functionality. Remove it if your whip shouldn't be chargeable.(Left it in for now, we'll see about this....)
			Item.noMelee = true;
			Item.noUseGraphic = true;
		}

	}
}
