using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{

    public class ReforgedOldMorningStar : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldMorningStar";
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.scale = 0.8f;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.damage = 13;
            Item.knockBack = 7f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 10;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 12000;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.OldMorningStar>();
        }
    }
}
