using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

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
            Item.useAnimation = 25;
            Item.autoReuse = true;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 33;
            Item.knockBack = 5;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Cyan;
            Item.shootSpeed = 1f;
            Item.scale = 0.8f;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 3 * 60);
            Projectile Spore = Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), target.Center, Vector2.Zero, ProjectileID.SporeTrap, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer);
            Spore.DamageType = DamageClass.Melee;
            Spore.damage /= 3;
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
