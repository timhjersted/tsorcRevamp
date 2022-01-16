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
            item.rare = ItemRarityID.Red;
            item.damage = 107;
            item.height = 32;
            item.autoReuse = true;
            item.knockBack = 8;
            item.maxStack = 1;
            item.melee = true;
            item.useAnimation = 21;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 15;
            item.value = PriceByRarity.Red_10;
            item.width = 40;
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
