using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

using tsorcRevamp.Items.Weapons.Melee.Axes;
namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemyForgottenRuneAxe : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Axes/ForgottenRuneAxe";

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
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.Gray;
        }
    }
}
