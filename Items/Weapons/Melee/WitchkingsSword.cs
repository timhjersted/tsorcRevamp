using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class WitchkingsSword : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Witchking's Sword");
        }
        public override void SetDefaults() {
            Item.rare = ItemRarityID.Red;
            Item.damage = 107;
            Item.height = 32;
            Item.autoReuse = true;
            Item.knockBack = 8;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 21;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = PriceByRarity.Red_10;
            Item.width = 40;
        }

        public override void OnHitNPC(Player player, NPC npc, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) {
                npc.AddBuff(BuffID.OnFire, 360, false);
            }
        }
        public override void MeleeEffects(Player player, Rectangle rectangle) {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }
}
