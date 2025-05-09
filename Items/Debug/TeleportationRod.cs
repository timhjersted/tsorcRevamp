using Terraria.GameContent.Creative;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Projectiles.Melee.Spears;
using tsorcRevamp.Projectiles.Summon.NullSprite;
using Microsoft.Xna.Framework;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.ForgottenImp;
using tsorcRevamp.Projectiles.Summon.EtherianWyvern;
using Terraria.Localization;
using System;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Magic;
using System.Media;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Debug
{
    class TeleportationRod : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Summon/EtherianWyvernStaff";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
        }
        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.knockBack = 1f;
            Item.width = 54;
            Item.height = 54;
            Item.useTime = Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
            Item.mana = 10;
            // Item.UseSound = SoundID.Item44;
            Item.DamageType = DamageClass.Summon;
            // Item.buffType = ModContent.BuffType<EtherianWyvernBuff>();
            Item.shoot = ModContent.ProjectileType<Nothing>();
            Item.noMelee = true;
        }
        public void LimitPointToPlayerReachableArea(ref Vector2 pointPoisition, Player player)
        {
            Vector2 center = player.Center;
            Vector2 vector = pointPoisition - center;
            float num = Math.Abs(vector.X);
            float num2 = Math.Abs(vector.Y);
            float num3 = 1f;
            if (num > 960f)
            {
                float num4 = 960f / num;
                if (num3 > num4)
                {
                    num3 = num4;
                }
            }
            if (num2 > 600f)
            {
                float num5 = 600f / num2;
                if (num3 > num5)
                {
                    num3 = num5;
                }
            }
            Vector2 vector2 = vector * num3;
            pointPoisition = center + vector2;
        }
        public override bool CanUseItem(Player player)
        {
            return Terraria.ModLoader.ModContent.GetInstance<tsorcRevampConfig>().DebugMode;
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            Vector2 pointPoisition = default(Vector2);
            pointPoisition.X = (float)Main.mouseX + Main.screenPosition.X;
            if (player.gravDir == 1f)
            {
                pointPoisition.Y = (float)Main.mouseY + Main.screenPosition.Y - (float)player.height;
            }
            else
            {
                pointPoisition.Y = Main.screenPosition.Y + (float)Main.screenHeight - (float)Main.mouseY;
            }
            pointPoisition.X -= player.width / 2;
            player.LimitPointToPlayerReachableArea(ref pointPoisition);
            if (!(pointPoisition.X > 50f) || !(pointPoisition.X < (float)(Main.maxTilesX * 16 - 50)) || !(pointPoisition.Y > 50f) || !(pointPoisition.Y < (float)(Main.maxTilesY * 16 - 50)))
            {
                return false;
            }
            int num = (int)(pointPoisition.X / 16f);
            int num2 = (int)(pointPoisition.Y / 16f);
            if (Collision.SolidCollision(pointPoisition, player.width, player.height))
            {
                return false;
            }
            player.Teleport(pointPoisition, 1);
            NetMessage.SendData(65, -1, -1, null, 0, player.whoAmI, pointPoisition.X, pointPoisition.Y, 1);
            return false;
        }

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ApprenticesWand>(), 1);
            recipe.AddCondition(tsorcRevampWorld.DebugModeEnabled);

            recipe.Register();
        }
    }
}