using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class Jawblade : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("'A blade of bone and fangs'" +
                "\nShoots out a homing skull upon hitting enemies with the blade"); */
        }

        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 76;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 40;
            Item.useTime = 80;
            Item.damage = 45;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ProjectileID.BookOfSkullsSkull;
            Item.shootSpeed = 9f;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Beige * 0.75f;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile Skull = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, UsefulFunctions.Aim(player.Center, target.Center, 10), ProjectileID.BookOfSkullsSkull, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer);
        }

        //TODO: Remove this
        public override bool CanShoot(Player player)
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<CalciumBlade>());
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
