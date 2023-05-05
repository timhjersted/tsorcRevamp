using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon {
    class SunsetQuasar : ModItem 
    {
        public override void SetStaticDefaults() 
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
            ItemID.Sets.StaffMinionSlotsRequired[Item.type] = 1;
        }
        public override void SetDefaults() 
        {
            Item.damage = 12;
            Item.knockBack = 3f;
            Item.mana = 10;
            Item.width = 42;
            Item.height = 28;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = Item.buyPrice(0, 50, 0, 0);
            Item.rare = ModContent.RarityType<CDW_SunsetQuasar>();
            Item.UseSound = SoundID.Item44;


            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
            Item.buffType = ModContent.BuffType<Buffs.Summon.SunsetQuasarBuff>();
            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarToken>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) 
        {
            position = Main.MouseWorld;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) 
        {
            player.AddBuff(Item.buffType, 2);

            var projectile = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
            projectile.originalDamage = Item.damage;
            return false;
        }
    }
    //Custom Draw Wavelength
    public class CDW_SunsetQuasar : ModRarity 
    {
        public override Color RarityColor => new Color(148, 179, 223);
    }
}
