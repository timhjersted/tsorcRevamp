using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>The Royal Knight's thunder bow: presentation only (the puppet fires RoyalThunderArrow itself).
    /// Permanently shares the Forgotten Thunder Bow art, 30x78, facing right; string column x6-7, y4-74; riser gem (24, 39).</summary>
    public class EnemyRoyalThunderBow : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ForgottenThunderBow));

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 30;
            Item.width = 30;
            Item.height = 78;
            Item.knockBack = 2f;
            Item.noMelee = true;
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 16f;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = 0;
        }
    }
}
