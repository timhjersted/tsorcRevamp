﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Specialist
{
    public class PiercingGaze : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 125;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 48;
            Item.height = 34;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true; //so the item's animation doesn't do damage
            Item.knockBack = 4;
            Item.value = 100000;
            Item.scale = 0.9f;
            Item.rare = ItemRarityID.LightRed;
            Item.crit = 5;
            //Item.UseSound = SoundID.Item40;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ranged.PiercingPlasma>();
            Item.shootSpeed = 22f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-10, 0);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                if (modPlayer.PiercingGazeCharge >= 16)
                {
                    modPlayer.PiercingGazeCharge = 0;
                    type = ModContent.ProjectileType<Projectiles.Ranged.PiercingGaze>();
                    damage = (int)(damage * 1.5f);
                    player.itemTime = 120;
                    player.itemAnimation = 120;
                }
            }
        }
        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Retinazer);
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ranged.PiercingGaze>()] == 0;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DamagedLaser>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddIngredient(ItemID.SoulofSight, 5);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
