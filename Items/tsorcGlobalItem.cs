using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Pets;

namespace tsorcRevamp.Items {
    public class tsorcGlobalItem : GlobalItem {

        public override void MeleeEffects(Item item, Player player, Rectangle hitbox) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.MiakodaCrescentBoost) {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 164, player.velocity.X * 1.2f, player.velocity.Y * 1.2f, 80, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }

            if (modPlayer.MiakodaNewBoost) {
                int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 57, player.velocity.X * 1.2f, player.velocity.Y * 1.2f, 120, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitNPC(Item item, Player player, NPC target, int damage, float knockBack, bool crit) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.MiakodaCrescentBoost) {
                target.AddBuff(ModContent.BuffType<Buffs.CrescentMoonlight>(), 240);
            }

            if (modPlayer.MiakodaNewBoost) {
                target.AddBuff(BuffID.Midas, 300);
            }
        }
    }
}