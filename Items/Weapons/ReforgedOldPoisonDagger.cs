using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class ReforgedOldPoisonDagger : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Has a 50% chance to poison the enemy");
        }

        public override void SetDefaults()
        {
            item.damage = 18;
            item.width = 22;
            item.height = 22;
            item.knockBack = 3;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1.1f;
            item.useAnimation = 11;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.Stabbing;
            item.useTime = 15;
            item.value = 500;
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                target.AddBuff(BuffID.Poisoned, 360);
            }
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.rand.Next(3) == 0)
            {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ToxicBubble, player.velocity.X * 1.2f + (float)(player.direction * 1.2), player.velocity.Y * 1.2f, 100, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldPoisonDagger"));
            //recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
