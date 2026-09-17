using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyArtoriasGreatsword : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ArtoriasGreatsword));

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = 1;
            Item.knockBack = 7f;
            Item.width = 74;
            Item.height = 74;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkViolet;
        }
    }
}
