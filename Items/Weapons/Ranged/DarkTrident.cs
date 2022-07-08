using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Weapons.Ranged
{
    class DarkTrident : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Pierces enemies with dark magic" +
                "\nLeft click to stab, hold right click to charge a throw" +
                "\nChanges between ranged or melee damage depending on which stat is higher" +
                "\nFully charged throws set hit enemies ablaze");
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.DarkTridentHeld>();
            Item.channel = true;

            Item.damage = 110;
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
            //Item.shoot = ModContent.ProjectileType<Projectiles.GoldSpear>();
            Item.channel = true;
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

            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<Projectiles.DarkTridentHeld>(), damage, knockback, player.whoAmI, type);
            return false;
        }


        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
    }
}
