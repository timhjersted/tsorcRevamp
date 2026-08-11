using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Axes 
{
    public class BrokenDualBladedAxe : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 66;
			Item.height = 64;
			Item.damage = 22;
			Item.knockBack = 9;
			Item.scale = 1f;
			Item.axe = 30;
			Item.rare = ItemRarityID.LightRed;
			Item.value = 12000;
            Item.UseSound = SoundID.Item1;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 38;
			Item.useAnimation = 38;
			Item.DamageType = DamageClass.Melee;
		}
	}
}
