using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldPoisonDagger : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldPoisonDagger";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Has a 50% chance to poison the enemy");
        }

        public override void SetDefaults() {
            Item.damage = 18;
            Item.width = 22;
            Item.height = 22;
            Item.knockBack = 3;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.1f;
            Item.useAnimation = 11;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Thrust;
            Item.useTime = 15;
            Item.value = 500;
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.Poisoned, 360);
            }
        }
        public override void MeleeEffects(Player player, Rectangle hitbox) {
            if (Main.rand.Next(3) == 0) {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ToxicBubble, player.velocity.X * 1.2f + (float)(player.direction * 1.2), player.velocity.Y * 1.2f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldPoisonDagger").Type);
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            
            recipe.Register();
        }
    }
}
