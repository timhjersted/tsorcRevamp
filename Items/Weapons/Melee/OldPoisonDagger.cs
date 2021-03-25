using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OldPoisonDagger : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 20" +
                                "\nMaximum damage is increased by damage modifiers" +
                                "\nHas a 50% chance to poison the enemy");
        }

        public override void SetDefaults() {
            item.damage = 24;
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

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.Poisoned, 360);
            }
        }
        public override void MeleeEffects(Player player, Rectangle hitbox) {
            if (Main.rand.Next(3) == 0) {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ToxicBubble, player.velocity.X * 1.2f + (float)(player.direction * 1.2f), player.velocity.Y * 1.2f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
