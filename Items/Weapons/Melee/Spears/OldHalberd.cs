using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    class OldHalberd : ModItem
    {
        public static int BaseDamage = 35;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Main.LocalPlayer.GetTotalDamage(DamageClass.Melee).ApplyTo(BaseDamage));
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = BaseDamage;
            Item.width = 60;
            Item.height = 60;
            Item.knockBack = 6;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 32;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useTime = 32;
            Item.value = 7000;
            Item.shootSpeed = 4f;
            Item.shoot = ModContent.ProjectileType<Nothing>();
            Item.useStyle = ItemUseStyleID.Swing;
        }

        public override bool AltFunctionUse(Player player)
        {
            if(!Main.mouseLeft && player.ItemTimeIsZero)
            {
                return true;
            } else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<OldHalberdProj>(), damage, knockback, player.whoAmI);
                return true;
            }
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noUseGraphic = false;
            Item.noMelee = false;
            return true;
        }
        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
