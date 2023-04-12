using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class PeculiarSphere : ModItem 
    {
        public override void SetStaticDefaults() 
        {
            // DisplayName.SetDefault("Peculiar Sphere");
            // Tooltip.SetDefault("Summons a friendly Owl Archer to fight for you.");

            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }
        public override void SetDefaults() 
        {
            Item.damage = 115;
            Item.knockBack = 1f;
            Item.width = 44;
            Item.height = 50;
            Item.useTime = Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.mana = 10;
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<Buffs.Summon.NondescriptOwlBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Archer.ArcherToken>();
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack) 
        {
            player.AddBuff(Item.buffType, 2);
            int p = Projectile.NewProjectile(source, position, speed, type, damage, knockBack);
            Main.projectile[p].originalDamage = Item.damage;
            return true;
        }
        /*public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedBar, 6);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }*/
    }
}