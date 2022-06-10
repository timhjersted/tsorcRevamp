using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyBioSpitBall : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.height = 24;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1; //was 8
            Projectile.tileCollide = true;
            Projectile.width = 24;
            Projectile.timeLeft = 80;
        }

        public override bool PreKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.04f}, Projectile.Center); //grass cut / acid singe sound
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 71, 0.3f, 0.3f, 200, default, 1f);
            Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 71, 0.2f, 0.2f, 200, default, 2f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 71, 0.2f, 0.2f, 200, default, 2f);
            Main.dust[dust].noGravity = false;
            //projectile.type = 96; //killpretendtype
            return true;
        }

        public override void AI()
        {
            //Custom sound, not vanilla sound:
            //Terraria.Audio.SoundEngine.PlaySound(SoundLoader.customSoundType, (int)position.X, (int)position.Y, mod.GetSoundSlot(SoundType.Custom, "tsorcRevamp/Sounds/Custom/[INSERTSOUNDEFFECTHERE]"));

            Projectile.rotation += 1f;
            if (Main.rand.Next(3) == 0)
            {
                //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 34); //try 5 (faint woosh), 20, was 17
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }


        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            
            target.AddBuff(BuffID.Blackout, 360, false); //darkness

            if (tsorcRevampWorld.Slain.ContainsKey(NPCID.EaterofWorldsHead))
            {
                target.AddBuff(20, 150, false); //poisoned
                target.AddBuff(30, 150, false); //bleeding
            }

            if (tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead))
            {
                target.AddBuff(70, 150, false); //acid venom
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 HP after several hits
                target.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 10;
            }
        }
    }
}

