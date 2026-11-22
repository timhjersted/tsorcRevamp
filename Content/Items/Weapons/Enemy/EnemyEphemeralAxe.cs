using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyEphemeralAxe : ModItem
    {
        // The held and recalled axe deliberately share the same permanent asset.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyEphemeralThrowingAxeProj));
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 60;
            Item.damage = 20;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 50;
            Item.useTime = 50;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.knockBack = 3f;
            Item.value = 0;
        }
    }
}
