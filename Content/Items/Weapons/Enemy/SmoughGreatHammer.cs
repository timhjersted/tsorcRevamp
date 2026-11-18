using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee.Hammers;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class SmoughGreatHammer : ModItem
    {
        private const int HammerPower = 80;

        public override void SetDefaults()
        {
            Item.width = 66;
            Item.height = 80;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 92;
            Item.knockBack = 11f;
            Item.hammer = HammerPower;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 42;
            Item.useTime = 42;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);

            tsorcInstancedGlobalItem melee = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            melee.slashColor = new Color(212, 174, 102);
            melee.WeaponPoiseMultiplier = 1.5f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SmoughHammerSlam>()] > 0)
            {
                return false;
            }

            if (player.altFunctionUse == 2)
            {
                if (player.mount.Active || player.gravDir != 1f || player.GetModPlayer<tsorcRevampPlayer>().isDodging)
                {
                    return false;
                }

                Item.shoot = ModContent.ProjectileType<SmoughHammerSlam>();
                Item.shootSpeed = 1f;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.hammer = 0;
                return true;
            }

            Item.shoot = ProjectileID.None;
            Item.shootSpeed = 0f;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.hammer = HammerPower;
            return true;
        }

        public override void HoldItem(Player player)
        {
            // Mask the vanilla swing and tile edits while the slam projectile owns the attack.
            bool slamming = player.ownedProjectileCounts[ModContent.ProjectileType<SmoughHammerSlam>()] > 0;
            Item.noMelee = slamming;
            Item.noUseGraphic = slamming;
            Item.hammer = slamming ? 0 : HammerPower;
            if (player.lastVisualizedSelectedItem.type == Type)
            {
                player.lastVisualizedSelectedItem.noUseGraphic = slamming;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<SoulOfSmough>();
            recipe.AddIngredient<DarkSoulItem>(25000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
