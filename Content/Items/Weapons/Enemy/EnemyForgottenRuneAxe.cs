using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Axes;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyForgottenRuneAxe : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ForgottenRuneAxe));

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = ForgottenRuneAxe.BaseDmg;
            Item.knockBack = 5f;
            Item.useAnimation = 28;
            Item.useTime = 28;
            Item.width = 46;
            Item.height = 38;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.Gray;
        }
    }
}
