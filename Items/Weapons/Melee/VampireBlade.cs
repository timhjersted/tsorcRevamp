using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class VampireBlade : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Heals the player when dealing damage to enemies");
        }
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 27;
            Item.useTime = 23;
            Item.damage = 42;
            Item.knockBack = 2;
            Item.scale = 1.1f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Melee;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 25);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            player.statLife += damage / 20;
            if (player.statLife > player.statLifeMax2)
            {
                player.statLife = player.statLifeMax2;
            }
        }

        public override void OnHitPvp(Player player, Player target, int damage, bool crit)
        {
            player.statLife += damage / 20;
            if (player.statLife > player.statLifeMax2)
            {
                player.statLife = player.statLifeMax2;
            }
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 54, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }
}
