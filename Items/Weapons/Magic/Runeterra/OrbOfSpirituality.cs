using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Magic.Runeterra;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Magic;
using Terraria.Audio;
using tsorcRevamp.Projectiles.Summon.Runeterra;
using tsorcRevamp.Items.Materials;
using Terraria.DataStructures;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfSpirituality : ModItem
    {
        public static Color FilledColor = Color.YellowGreen;
        public static int DashBuffDuration = 15;
        public static int DashCD = 60;
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 8));
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = false;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.damage = 330;
            Item.mana = 60;
            Item.knockBack = 8;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Cyan;
            Item.shootSpeed = OrbOfDeception.ShootSpeed;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<OrbOfSpiritualityOrb>();
            Item.holdStyle = ItemHoldStyleID.HoldLamp;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            Item.mana = 60;
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] != 0)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityFlame>();
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] == 0 && player.GetModPlayer<tsorcRevampPlayer>().EssenceThief < 9)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityOrb>();
            }
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<OrbOfSpiritualityCharm>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>())) //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] == 0 && player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrbIdle>()] == 0)
            {
                Projectile.NewProjectile(Projectile.InheritSource(player), player.Center, Vector2.Zero, ModContent.ProjectileType<OrbOfSpiritualityOrbIdle>(), 0, 0);
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void UpdateInventory(Player player)
        {
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfFlame>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
