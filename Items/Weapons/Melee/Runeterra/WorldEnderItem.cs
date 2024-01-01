using Microsoft.Xna.Framework;
using Steamworks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder;
using tsorcRevamp.Projectiles.Melee.Swords;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    [Autoload(false)]
    public class WorldEnderItem : ModItem 
    { 
        public const float TimeWindow = 3;
        public const float FirstSwingCooldown = 4;
        public const float SecondSwingCooldown = 9;
        public const float ThirdSwingCooldown = 15;
        public override void SetDefaults()
        {
            Item.width = 132;
            Item.height = 132;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 500;
            Item.knockBack = 20f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 100f;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Color.DarkRed;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<WorldEnderCooldown>()))
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override float UseSpeedMultiplier(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                return 0.5f;
            }
            return base.UseSpeedMultiplier(player) * player.GetTotalAttackSpeed(DamageClass.Melee);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2) //shoot Nothing
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = false;
                return true;
            }
            Item.noMelee = true;
            switch (player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing)
            {
                case 1:
                    {
                        Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<WorldEnderSwordSwing1>(), damage, knockback, Main.myPlayer);
                        player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing++;
                        break;
                    }
                case 2:
                    {
                        Projectile.NewProjectile(source, position, velocity * 0.6f, ModContent.ProjectileType<WorldEnderSwordSwing2>(), damage, knockback, Main.myPlayer);
                        player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing++;
                        break;
                    }
                case 3:
                    {
                        Projectile.NewProjectile(source, position, velocity * 0.6f, ModContent.ProjectileType<WorldEnderSwordSwing3>(), damage, knockback, Main.myPlayer);
                        player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing = 1;
                        break;
                    }
            }
            return true;
        }
    }
}
