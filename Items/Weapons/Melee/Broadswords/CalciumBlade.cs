using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    [LegacyName("BoneBlade")]
    public class CalciumBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("'A blade of sharpened bone'" +
                "\nShoots out a bone upon hitting enemies with the blade"); */
        }

        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 78;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 33;
            Item.useTime = 33;
            Item.damage = 36;
            Item.knockBack = 3.3f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Beige * 0.75f;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile Bone = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, UsefulFunctions.Aim(player.Center, target.Center, 10), ProjectileID.Bone, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer);
            Bone.DamageType = DamageClass.Melee;
            Bone.damage -= Bone.damage / 4;
            Bone.netUpdate = true;
        }

        //TODO: Remove this
        public override bool CanShoot(Player player)
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Bone, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
