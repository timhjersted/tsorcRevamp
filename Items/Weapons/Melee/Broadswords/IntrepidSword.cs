using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class IntrepidSword : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Only those who walk lightly can master this sword's true power" +
                                "\n[c/ffbf00:Damage is reduced from its full power with higher defense.]"); */
        }

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 62;
            Item.useAnimation = 23;
            Item.useTime = 23;
            Item.damage = 200;
            Item.knockBack = 3;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Lime_7;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, color, 1.9f);
            Main.dust[dust].noGravity = true;
        }


        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage *= (-0.01f * player.statDefense) + 1;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Galaxia>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
