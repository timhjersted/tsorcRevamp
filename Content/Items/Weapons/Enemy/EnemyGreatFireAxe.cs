using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Axes;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyGreatFireAxe : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GreatFireAxe));

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 22;
            Item.knockBack = 10f;
            Item.useAnimation = 37;
            Item.useTime = 37;
            Item.width = 72;
            Item.height = 64;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.OrangeRed;
        }
    }
}
