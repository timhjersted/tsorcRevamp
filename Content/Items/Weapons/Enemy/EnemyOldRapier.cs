using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyOldRapier : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Weapons/Enemy/OldRapier";

        public override void SetDefaults()
        {
            Item.damage = 24;
            Item.DamageType = DamageClass.Melee;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 2f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}
