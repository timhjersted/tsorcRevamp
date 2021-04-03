using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class ForgottenPoisonAxe : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The blade has been dipped in poison.");
        }
        public override void SetDefaults() {

            item.rare = ItemRarityID.LightRed;
            item.damage = 76;
            item.height = 46;
            item.knockBack = 5;
            item.melee = true;
            item.scale = 1.2f;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 15;
            item.value = 40000000;
            item.width = 50;
        }

        public override void OnHitNPC(Player player, NPC npc, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) {
                npc.AddBuff(BuffID.Poisoned, 360, false);
            }
        }
    }
}
