using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Axes 
// Forgotten City drop, target 90 DPS
{
    public class DualBladedAxe : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 66;
			Item.height = 64;
			Item.damage = 48;
			Item.knockBack = 11;
			Item.scale = 1f;
			Item.axe = 80;
			Item.rare = ItemRarityID.LightRed;
			Item.value = 12000;
            Item.UseSound = SoundID.Item1;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 32;
			Item.useAnimation = 32;
			Item.DamageType = DamageClass.Melee;
		}
	}
}
