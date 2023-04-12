using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class ThornDecapitator : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Thorn Decapitator");
            // Tooltip.SetDefault("Creates spore clouds on top of struck enemies");

        }
        public int shootstacks = 0;

        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 80;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 33;
            Item.autoReuse = true;
            Item.useTime = 33;
            Item.maxStack = 1;
            Item.damage = 40;
            Item.knockBack = 5;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 1f;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            //player.Center + Main.rand.NextVector2CircularEdge(100, 100)
            if (crit)
            Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), target.Center, Vector2.Zero, ProjectileID.SporeTrap, (int)(damage * 0.375f), 0, Main.myPlayer);
            else 
            Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), target.Center, Vector2.Zero, ProjectileID.SporeTrap, (int)(damage * 0.5f), 0, Main.myPlayer);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.BladeofGrass);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
