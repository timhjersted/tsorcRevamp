using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.TripleThreat;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class TripleThreat : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults()
        {
            Item.damage = 70;
            Item.knockBack = 3f;
            Item.mana = 10;
            Item.width = 42;
            Item.height = 28;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.UseSound = SoundID.Item44;

            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<TripleThreatBuff>();
            Item.shoot = ModContent.ProjectileType<FriendlyRetinazer>();
        }

        int triadType;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = Main.MouseWorld;

            if (triadType == 0)
            {
                type = ModContent.ProjectileType<FriendlyRetinazer>();
            }
            if (triadType == 1)
            {
                type = ModContent.ProjectileType<FriendlySpazmatism>();
            }
            if (triadType == 2)
            {
                type = ModContent.ProjectileType<FriendlyCataluminance>();
            }

            triadType++;
            if (triadType == 3)
            {
                triadType = 0;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // This is needed so the buff that keeps your minion alive and allows you to despawn it properly applies
            player.AddBuff(Item.buffType, 2);

            // Minions have to be spawned manually, then have originalDamage assigned to the damage of the summon item
            if (Main.myPlayer == player.whoAmI)
            {
                var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
                projectile.originalDamage = Item.damage;
            }

            // Since we spawned the projectile manually already, we do not need the game to spawn it for ourselves anymore, so return false
            return false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DamagedRemote>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddIngredient(ItemID.SoulofSight, 5);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}