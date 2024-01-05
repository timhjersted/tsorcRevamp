using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;

namespace tsorcRevamp.Items.Weapons.Summon
{
    [Autoload(false)]
    public class BeetleIdol : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 4;
        }
        public override void SetDefaults()
        {
            Item.damage = 400;
            Item.knockBack = 3f;
            Item.mana = 10;
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 40, 0, 0);
            Item.UseSound = SoundID.Item44;
            Item.rare = ItemRarityID.Purple;

            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<SamuraiBeetleBuff>();
            Item.shoot = ModContent.ProjectileType<SamuraiBeetleProjectile>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
            position = Main.MouseWorld;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // This is needed so the buff that keeps your minion alive and allows you to despawn it properly applies
            player.AddBuff(Item.buffType, 2);

            // Minions have to be spawned manually, then have originalDamage assigned to the damage of the summon item
            if (player.ownedProjectileCounts[Item.shoot] < 1)
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
            recipe.AddIngredient(ItemID.LunarBar, 5);
            recipe.AddIngredient(ItemID.BeetleWings);
            recipe.AddIngredient(ItemID.BeetleHusk, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 88000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}