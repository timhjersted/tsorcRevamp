using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyYellowTail : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(YellowTail));

        public override void SetDefaults()
        {
            Item.damage = YellowTail.BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.width = 46;
            Item.height = 46;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.knockBack = 2.6f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.Yellow;
        }
    }
}
