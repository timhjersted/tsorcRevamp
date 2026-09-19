using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Electrocute
{
    class ElectrocuteProjectile : ModProjectile
    {
        public const int Frames = 7;
        public const int Lifespan = 60;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Frames;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 80;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 0.6f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = Lifespan;
        }

        public bool AppliedOnSpawn = false;
        public override void AI()
        {
            if (!AppliedOnSpawn)
            {
                Projectile.CritChance = (int)Projectile.ai[0];
                int soundRoll = Main.rand.Next(3) + 1;
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(Electrocute)) + "_Proc" + soundRoll)
                    with
                    {
                        Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.5f
                    }, 
                    Projectile.Center);
                AppliedOnSpawn = true;
            }

            float frameDivisor = (float)Lifespan / Frames;
            Projectile.frame = Frames - (int)(Projectile.timeLeft / (float)frameDivisor);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
            List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
    }
}
