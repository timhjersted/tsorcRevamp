using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Melee;
using System.Collections.Generic;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    [Autoload(true)]
    public class OrbOfDeception : ModItem
    {
        public static Color FilledColor = Color.YellowGreen;
        public static float OrbDmgMod = 125f;
        public static float OrbReturnDmgMod = 150f;
        public static float DmgLossOnPierce = 5f;
        public static float FilledOrbDmgMod = 160f;
        public static float ShootSpeed = 20f;
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.damage = 20;
            Item.mana = 25;
            Item.knockBack = 8;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = ShootSpeed;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfDeceptionOrb>();
            Item.holdStyle = ItemHoldStyleID.HoldLamp;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfDeceptionOrb>()] != 0)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionFlame>();
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfDeceptionOrb>()] == 0)
            {
                type = ModContent.ProjectileType<OrbOfDeceptionOrb>();
            }
        }
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfDeceptionOrb>()] == 0 && player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfDeceptionOrbIdle>()] == 0) 
            {
                Projectile.NewProjectile(Projectile.InheritSource(player), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbOfDeceptionOrbIdle>(), 0, 0); 
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowOrb);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
