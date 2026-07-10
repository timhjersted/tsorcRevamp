using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Axes // Code modified from Zero-Exodus's code :)
{
    public class DualBladedAxe : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 46;
			Item.height = 44;
			Item.damage = 35;
			Item.knockBack = 11;
			Item.scale = 1f;
			Item.axe = 80;
			Item.rare = ItemRarityID.LightRed;
			Item.value = 12000;
            Item.UseSound = SoundID.Item1;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 23;
			Item.useAnimation = 27;
			Item.DamageType = DamageClass.Melee;
		}
	}
}
