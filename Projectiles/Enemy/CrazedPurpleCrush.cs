using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
    class CrazedPurpleCrush : ModProjectile
    {
        public override void SetDefaults()
        {

            projectile.width = 16;
            //projectile.aiStyle = 24;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.damage = 25;
            //projectile.aiPretendType = 94;
            //projectile.timeLeft = 100;
            projectile.light = 0.8f;
            Main.projFrames[projectile.type] = 1;

            drawOriginOffsetX = 12;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Purple Crush");
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 44; //killpretendtype
            return true;
        }

        public override void AI()
        {

            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y - 10), projectile.width, projectile.height, DustID.Shadowflame, 0, 0, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;

            projectile.rotation++;

            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10)
            {
           //     projectile.velocity.X *= 1.01f;
           //     projectile.velocity.Y *= 1.01f;
            }

           // Rectangle projrec = new Rectangle((int)projectile.position.X + (int)projectile.velocity.X, (int)projectile.position.Y + (int)projectile.velocity.Y, projectile.width, projectile.height);
           // Rectangle prec = new Rectangle((int)Main.player[Main.myPlayer].position.X, (int)Main.player[Main.myPlayer].position.Y, (int)Main.player[Main.myPlayer].width, (int)Main.player[Main.myPlayer].height);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }
            Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 300 / buffLengthMod, false); //poisoned
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.Crippled>(), 300 / buffLengthMod, false); //crippled
            Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 300 / buffLengthMod, false); //bleeding
        }
    }
}