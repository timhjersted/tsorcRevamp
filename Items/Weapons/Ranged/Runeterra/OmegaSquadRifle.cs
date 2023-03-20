using tsorcRevamp.Projectiles.Ranged.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class OmegaSquadRifle : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Omega Squad Rifle");
            Tooltip.SetDefault("Converts seeds into Radioactive Darts which inflict Irradiated" +
                "\nIrradiated damage scales with ranged damage" +
                "\nAlso uses all darts as ammo" +
                "\nRight click to shoot a homing blinding dart which inflicts confusion" +
                "\nPress Special Ability hotkey to drop a Nuclear Mushroom" +
                "\nMushroom explodes on contact and applies an even higher dose of radiation" +
                "\n'There's a mushroom out there with your name on it'");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 8;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item64;//63
            Item.DamageType = DamageClass.Ranged; 
            Item.damage = 123;
            Item.knockBack = 1f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.Seed;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Dart;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, -10f);
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.Seed & player.altFunctionUse == 1)
            {
                type = ModContent.ProjectileType<RadioactiveDart>();
            }
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<RadioactiveBlindingLaser>();
            }
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (Main.mouseRight & !Main.mouseLeft & !player.HasBuff(ModContent.BuffType<RadioactiveBlindingLaserCooldown>()))  //cooldown gets applied on projectile spawn
            {
                player.altFunctionUse = 2;
            }
            if (Main.mouseLeft)
            {
                player.altFunctionUse = 1;
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2 || !player.HasBuff(ModContent.BuffType<RadioactiveBlindingLaserCooldown>()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<AlienRifle>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}