using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Presentation-only bow the Assassin carries and shoots. Deliberately draws the player Elfin Bow's sprite
    /// (permanent texture sharing, not a placeholder): 14x58, facing right, riser x6..13 / y24..33 (grip centre 10, 29),
    /// string = the 2px outline column x0..1 / y10..47 between the curled limb tips, colour (3, 37, 11).</summary>
    public class EnemyAssassinBow : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ElfinBow));

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 58;
            Item.damage = 1;
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item5;
            Item.value = 0;
        }
    }
}
