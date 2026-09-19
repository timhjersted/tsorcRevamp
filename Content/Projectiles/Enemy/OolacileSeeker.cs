using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class OolacileSeeker : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Oolacile Seeker");
        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.width = 34;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 750;
            Main.projFrames[Projectile.type] = 4;
            Projectile.light = 1;
            Projectile.scale = 1.22f;
        }

        public override void AI()
        {
            Projectile.rotation++;

            this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);

            if (this.Projectile.timeLeft < 100)
            {
                this.Projectile.scale *= 0.9f;
                this.Projectile.damage = 0;
            }

            if (this.Projectile.timeLeft > 200 && this.Projectile.timeLeft < 500)
            {
                this.Projectile.velocity.X -= (this.Projectile.position.X - Main.player[(int)this.Projectile.ai[0]].position.X) / 1000f;
                this.Projectile.velocity.Y -= (this.Projectile.position.Y - Main.player[(int)this.Projectile.ai[0]].position.Y) / 1000f;

                this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);
                this.Projectile.velocity.Y = (float)Math.Sin(this.Projectile.rotation) * 8;
                this.Projectile.velocity.X = (float)Math.Cos(this.Projectile.rotation) * 8;
            }




            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 54, 0, 0, 100, Color.Black, 1.0f);
            Main.dust[dust].noGravity = true;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
        }

        // A real death event. This replaces the old PreKill, which reassigned Projectile.type to vanilla 41
        // so the engine would play THAT projectile's death instead — the seeker had no death of its own.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 70; i++)
            {
                Vector2 outward = Main.rand.NextVector2Circular(5f, 5f);
                Dust shard = Dust.NewDustPerfect(Projectile.Center, DustID.Wraith, outward, 90, Color.Black,
                    Main.rand.NextFloat(1f, 1.8f));
                shard.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.expertMode)
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Darkness, 9000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 1800, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 9000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 300, false);
            }
            else
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Darkness, 18000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Poisoned, 3600, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 18000, false);
                Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 600, false);
            }
        }
    }
}