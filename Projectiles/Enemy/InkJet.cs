using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class InkJet : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 90;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
        }


        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3());
            if (Main.GameUpdateCount % 4 == 0)
            {                
                Dust thisDust = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Asphalt, Projectile.velocity.X, Projectile.velocity.Y, 0, new Color(), 4)];
                thisDust.noGravity = true;

                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }
        }


        public override void OnHitPlayer(Player target, int damage, bool crit) {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.Slow, 180 / buffLengthMod, false);
            target.AddBuff(BuffID.BrokenArmor, 180 / buffLengthMod, false);
            target.AddBuff(BuffID.Blackout, 600 / buffLengthMod, false);
            target.AddBuff(BuffID.Venom, 600 / buffLengthMod, false);
            target.AddBuff(BuffID.Obstructed, 120 / buffLengthMod, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
