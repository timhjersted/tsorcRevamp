using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class AncientFireSword : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            //item.prefixType=121;
            Item.rare = ItemRarityID.Green;
            Item.damage = 15;
            Item.width = 34;
            Item.height = 38;
            Item.knockBack = 4f;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldBroadsword);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60, false);
            Projectile Flamethrower = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, UsefulFunctions.Aim(player.Center, target.Center, 2f), ProjectileID.Flames, (int)(player.GetTotalDamage(DamageClass.Melee).ApplyTo((float)Item.damage / 4f)), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer);
            Flamethrower.DamageType = DamageClass.Melee;
            Flamethrower.ai[0] = 2f; //inflicts OnFire instead of Hellfire and for less time
            Flamethrower.usesLocalNPCImmunity = false;
            Flamethrower.localNPCHitCooldown = 0;
            Flamethrower.usesIDStaticNPCImmunity = true;
            Flamethrower.idStaticNPCHitCooldown = 20;
            Flamethrower.ArmorPenetration = 0;
            Flamethrower.netUpdate = true;
        }

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
