using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon
{
    public class PeculiarSphere : ModItem
    {
        public const float ScalingPerSlot = 0.15f;
        public const int DoubleShotMinimumSlots = 4;
        public const int BaseCritChance = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)System.Math.Round(ScalingPerSlot * 100), DoubleShotMinimumSlots);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = 65;
            Item.crit = BaseCritChance;
            Item.knockBack = 1f;
            Item.width = 44;
            Item.height = 50;
            Item.useTime = Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 25, 0, 0);
            Item.mana = 10;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<NondescriptOwlBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Archer.ArcherToken>();
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            player.AddBuff(Item.buffType, 2);
            if (Main.myPlayer == player.whoAmI)
            {
                int p = Projectile.NewProjectile(source, position, speed, type, damage, knockBack);
                Main.projectile[p].originalDamage = Item.damage;
            }
            return false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.PygmyStaff);
            recipe.AddIngredient(ItemID.LightShard);
            recipe.AddIngredient(ItemID.SoulofMight);
            recipe.AddIngredient(ItemID.SoulofSight);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}