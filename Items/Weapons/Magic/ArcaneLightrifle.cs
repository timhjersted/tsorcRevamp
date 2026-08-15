using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class ArcaneLightrifle : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Charges a focused beam of piercing light" +
                "\nReflects up to two times, massively amplifying its damage with each"); */
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 135;
            Item.autoReuse = true;
            Item.knockBack = (float)4;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.shootSpeed = (float)10;
            Item.crit = 2;
            Item.mana = 50;
            Item.noMelee = true;
            Item.value = PriceByRarity.Purple_11;
            Item.channel = true;

            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.ArcaneLightrifle>();
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 0);
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            float staminaCost = (30 * modPlayer.WeaponStaminaMult) / player.GetWeaponAttackSpeed(player.HeldItem);
            if (player.statMana <= player.GetManaCost(player.HeldItem) && player.GetModPlayer<tsorcRevampPlayer>().SoulsMode && !player.GetModPlayer<CeruleanFlaskPlayer>().IsCeruleanRestoring && !player.GetModPlayer<CeruleanFlaskPlayer>().IsDrinking)
            {
                MethodSwaps.TryUseQuickMana(player);
            }
            if (player.statMana <= player.GetManaCost(player.HeldItem) || (player.GetModPlayer<tsorcRevampPlayer>().SoulsMode && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < staminaCost))
            {
                return false;
            }
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Magic.ArcaneLightrifle>()] <= 0;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            //recipe.AddIngredient(ItemID.LaserMachinegun, 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 10);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
