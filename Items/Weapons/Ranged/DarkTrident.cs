using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class DarkTrident : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<DarkTridentHeld>();
            Item.channel = true;

            Item.damage = 90;
            Item.width = 24;
            Item.height = 48;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 4f;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
            Item.UseSound = SoundID.Item7;
            Item.shootSpeed = 24f;
            Item.channel = true;            
        }

        public override bool CanUseItem(Player player)
        {
            //Block using the item unless they have one more than the required stamina
            //Prevents a bug where, if the player uses this weapon with *exactly* the stamina required, it instantly throws it without letting them charge up
            //This happens constantly if they hold left mouse, as it gets used the instant stamina refills to that level
            int staminaUse = (int)(Item.useAnimation / player.GetAttackSpeed(Item.DamageType));
            staminaUse = (int)tsorcRevampPlayer.ReduceStamina(staminaUse);
            if(player.altFunctionUse != 2 && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < staminaUse * 2)
            {
                return false;
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.GetTotalDamage(DamageClass.Ranged).ApplyTo(100) < player.GetTotalDamage(DamageClass.Melee).ApplyTo(100))
            {
                Item.DamageType = DamageClass.Melee;
            }
            else
            {
                Item.DamageType = DamageClass.Ranged;
            }
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Thrust;
            }
            else
            {
                Item.useStyle = ItemUseStyleID.HoldUp;
            }

            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<DarkTridentHeld>(), damage, knockback, player.whoAmI, type);
            return false;
        }


        public override bool AltFunctionUse(Player player)
        {
            if (!Main.mouseLeft && player.ItemTimeIsZero)
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
    }
}
