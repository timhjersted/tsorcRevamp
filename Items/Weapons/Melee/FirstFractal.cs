using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class FirstFractal : ModItem {

        public override void SetDefaults() {
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.width = 24;
			item.height = 24;
			item.noUseGraphic = true;
			item.UseSound = SoundID.Item1;
			item.autoReuse = true;
			item.melee = true;
			item.channel = true;
			item.noMelee = true;
			item.shoot = ModContent.ProjectileType<Projectiles.FirstFractal>();
			item.useAnimation = 35;
			item.useTime = item.useAnimation / 5;
			item.shootSpeed = 16f;
			item.damage = 190;
			item.knockBack = 6.5f;
			item.value = Item.sellPrice(0, 20);
			item.crit = 10;
			item.rare = ItemRarityID.Red;
			item.glowMask = 271;
		}

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            Vector2 vector30 = Main.MouseWorld;
            List<NPC> validTargets2;
            bool sparkleGuitarTarget2 = GetSparkleGuitarTarget(out validTargets2, player);
            if (sparkleGuitarTarget2) {
                NPC nPC2 = validTargets2[Main.rand.Next(validTargets2.Count)];
                vector30 = nPC2.Center + nPC2.velocity * 20f;
            }
            Vector2 vector31 = vector30 - player.Center;
            Vector2 vector32 = Main.rand.NextVector2CircularEdge(1f, 1f);
            float num74 = 1f;
            int num75 = 1;
            for (int num76 = 0; num76 < num75; num76++) {
                if (!sparkleGuitarTarget2) {
                    vector30 += Main.rand.NextVector2Circular(24f, 24f);
                    if (vector31.Length() > 700f) {
                        vector31 *= 700f / vector31.Length();
                        vector30 = player.Center + vector31;
                    }
                    float num77 = tsorcRevamp.GetLerpValue(0f, 6f, player.velocity.Length(), clamped: true) * 0.8f;
                    vector32 *= 1f - num77;
                    vector32 += player.velocity * num77;
                    vector32 = vector32.SafeNormalize(Vector2.UnitX);
                }
                float num78 = 60f;
                float num79 = Main.rand.NextFloatDirection() * (float)Math.PI * (1f / num78) * 0.5f * num74;
                float num80 = num78 / 2f;
                float num81 = 12f + Main.rand.NextFloat() * 2f;
                Vector2 vector33 = vector32 * num81;
                Vector2 vector34 = new Vector2(0f, 0f);
                Vector2 vector35 = vector33;
                for (int num82 = 0; (float)num82 < num80; num82++) {
                    vector34 += vector35;
                    vector35 = vector35.RotatedBy(num79);
                }
                Vector2 vector36 = -vector34;
                Vector2 vector37 = vector30 + vector36;
                float lerpValue2 = tsorcRevamp.GetLerpValue(player.itemAnimationMax, 0f, player.itemAnimation, clamped: true);
                Projectile.NewProjectile(vector37, vector33, ModContent.ProjectileType<Projectiles.FirstFractal>(), item.damage, item.knockBack, item.owner, num79, lerpValue2);
            }
            return false;
        }
        public bool GetSparkleGuitarTarget(out List<NPC> validTargets, Player player) {
            validTargets = new List<NPC>();
            Rectangle value = Utils.CenteredRectangle(player.Center, new Vector2(1000f, 800f));
            for (int i = 0; i < 200; i++) {
                NPC nPC = Main.npc[i];
                if (nPC.CanBeChasedBy(player) && nPC.Hitbox.Intersects(value)) {
                    validTargets.Add(nPC);
                }
            }
            if (validTargets.Count == 0) {
                return false;
            }
            return true;
        }
    }
}
