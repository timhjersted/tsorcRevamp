using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class FireSpiritTome4 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tome of the Dying Star");
            /* Tooltip.SetDefault("Leave nothing but ash in your wake." +
                "\nLeft click to charge a detonating core" +
                "\nRight click to fire a rapid barrage of solar flares"); */
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.damage = 2000;
            Item.knockBack = 90;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.rare = ModContent.RarityType<OrangeRed>();
            Item.shootSpeed = 44;
            Item.mana = 50;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.DyingStarHoldout>();
            Item.channel = true;
            Item.noMelee = true;

        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            if (player.altFunctionUse == 2)
            {
                mult = 0.8f;
            }
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            var staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
            float staminaCost = (30 * modPlayer.WeaponStaminaMult) / player.GetWeaponAttackSpeed(player.HeldItem);
            int manaCost = player.GetManaCost(player.HeldItem);
            if (player.statMana <= manaCost)
            {
                if (!player.GetModPlayer<CeruleanFlaskPlayer>().IsCeruleanRestoring && !player.GetModPlayer<CeruleanFlaskPlayer>().IsDrinking && player.manaFlower)
                {
                    MethodSwaps.TryUseQuickMana(player);
                }
                if (player.statMana <= manaCost)
                {
                    return false;
                }
            }

            if ((player.GetModPlayer<tsorcRevampPlayer>().SoulsMode && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < staminaCost))
            {
                return false;
            }
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.DyingStarHoldout>()] <= 0;
        }

        public override bool? UseItem(Player player)
        {
            return base.UseItem(player);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FireSpiritTome3>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfBlight>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
