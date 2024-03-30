using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Projectiles.Summon.Runeterra.Dragons.GrandComet;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class RuneBladeRune : ModProjectile
    {
        public int Frames = 5;
        public int ProjectileLifetime = 120;
        public int torchColor = TorchID.Red;
        public Color DustColor = Color.Red;
        public float Timer = 0;
        public int TransparencyDivisor = 80;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.frame = Main.rand.Next(Frames);
            Projectile.penetrate = 4;
            Projectile.tileCollide = false;
            switch (Projectile.frame)
            {
                case 0:
                    {
                        torchColor = TorchID.Red;
                        DustColor = Color.Red;
                        break;
                    }
                case 1:
                    {
                        torchColor = TorchID.Blue;
                        DustColor = Color.Blue;
                        break;
                    }
                case 2:
                    {
                        torchColor = TorchID.Green;
                        DustColor = Color.Green;
                        break;
                    }
                case 3:
                    {
                        torchColor = TorchID.UltraBright;
                        DustColor = Color.LightSkyBlue;
                        break;
                    }
                case 4:
                    {
                        torchColor = TorchID.Orange;
                        DustColor = Color.Orange;
                        break;
                    }
            }
        }
        public override void AI()
        {
            if (Projectile.timeLeft > 20 && Timer < TransparencyDivisor)
            {
                Timer++;
            }
            else if (Projectile.timeLeft < 20)
            {
                Timer -= 4;
            }
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, torchColor);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = new Color(Timer / TransparencyDivisor, Timer / TransparencyDivisor, Timer / TransparencyDivisor, Timer / TransparencyDivisor);
            return base.PreDraw(ref lightColor);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.MageNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= RuneBlade.MageDmgAmp;
            }
        }
        public override void OnKill(int timeLeft)
        {
            Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.MagicMirror, 0f, 0f, 250, DustColor, 1f);
        }
    }
}