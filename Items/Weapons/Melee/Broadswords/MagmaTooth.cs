using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee;
using tsorcRevamp.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class MagmaTooth : ModItem
    {
        public int HitCounter = 0;
        public const int HitsNeeded = 8;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 58;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 33;
            Item.useTime = 33;
            Item.scale = 1.25f;
            Item.damage = 44;
            Item.crit = 4;
            Item.knockBack = 7.5f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HitCounter++;
            target.AddBuff(BuffID.OnFire3, 300, false);
            if (HitCounter >= HitsNeeded)
            {
                Projectile.NewProjectile(Item.GetSource_FromThis(), target.Center + new Vector2(0, 30), Vector2.Zero, ModContent.ProjectileType<MagmaToothVolcano>(), (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer, player.GetTotalCritChance(DamageClass.Melee) + Item.crit);
                HitCounter -= HitsNeeded;
            }
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 6, player.velocity.X, player.velocity.Y, 100, default, 2f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FieryGreatsword);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
