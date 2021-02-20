using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class AntimatRifle : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Antimat Rifle");
            Tooltip.SetDefault("Unbelievable damage at the cost of a 2.5 second cooldown between shots. \n" +
                                "Fires piercing high-velocity rounds.\n" +
                                "Uses Musket Balls as ammo.\n" +
                                "Musket Ball damage increases with enemy armor.\n" +
                                "(Does not work in PVP)");

	}

        public override void SetDefaults()
        {
            
            //item.prefixType=96;
            item.autoReuse = true;
            item.damage = 2000;
            item.width = 78;
            item.height = 26;
            item.knockBack = (float)5;
            item.maxStack = 1;
            item.noMelee = true;
            item.rare = ItemRarityID.Pink;
            item.scale = (float)0.9;
            item.useAmmo = AmmoID.Bullet;
            item.ranged = true;
            item.shoot = AmmoID.Bullet;
            item.shootSpeed = (float)20;
            //item.pretendType=96;
            item.useAnimation = 150;
            item.useTime = 150;
            item.UseSound = SoundID.Item36;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 25000000;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.SniperRifle, 1);
            recipe.AddIngredient(mod.GetItem("DestructionElement"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfChaos"), 1);
            recipe.AddIngredient(mod.GetItem("Humanity"), 20);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 100);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 240000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UseStyle( Terraria.Player player )
        {
            float backX = 24f; // move the weapon back
            float downY = 0f; // and down
            float cosRot = (float)Math.Cos(player.itemRotation);
            float sinRot = (float)Math.Sin(player.itemRotation);
            //Align
            player.itemLocation.X = player.itemLocation.X - (backX * cosRot * player.direction) - (downY * sinRot * player.gravDir);
            player.itemLocation.Y = player.itemLocation.Y - (backX * sinRot * player.direction) + (downY * cosRot * player.gravDir);
        }
        public override bool Shoot( Terraria.Player P, ref Vector2 Pos, ref float speedX, ref float speedY, ref int type, ref int DMG, ref float KB )
        {//as usual, ty Yoraiz0r
            if (type == AmmoID.Bullet) { type = mod.ProjectileType("AntiMaterialRound"); }

            Terraria.Projectile.NewProjectile(Pos.X, Pos.Y, speedX, speedY, type, DMG, KB, P.whoAmI);
            return false;
        }

    }
}
