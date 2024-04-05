using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.SamuraiBeetle
{
    public class SamuraiBeetleLightning : ModProjectile
    {
        public float Volume = 0.3f;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        SoundStyle ThunderLoopStyle;
        SlotId ThunderLoopID;
        public bool isFadingOut = false;
        public int SoundCount = 0;
        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 2000;
            Projectile.friendly = true;
            Projectile.timeLeft = 272;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Summon;
            ThunderLoopStyle = new SoundStyle(SamuraiBeetleProjectile.SoundPath + "heavy-thunder-loop") with { Volume = Volume, MaxInstances = 1 };
        }
        public override void OnSpawn(IEntitySource source)
        {
            if (!SoundEngine.TryGetActiveSound(ThunderLoopID, out var ActiveSound))
            {
                ThunderLoopID = SoundEngine.PlaySound(ThunderLoopStyle);
                SoundCount++;
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(ThunderLoopID, out var ActiveSound))
            {
                ActiveSound.Stop();
            }
        }
        public bool SoundPlayed = false;
        public override void AI()
        {
            if (!SoundEngine.TryGetActiveSound(ThunderLoopID, out var ActiveSound) && SoundCount < 2)
            {
                SoundCount++;
                ThunderLoopID = SoundEngine.PlaySound(ThunderLoopStyle);
            }
            else if (SoundEngine.TryGetActiveSound(ThunderLoopID, out var activeSound) && !SoundPlayed && Projectile.timeLeft == 121)
            {
                activeSound.Stop();
                ThunderLoopStyle = new SoundStyle(SamuraiBeetleProjectile.SoundPath + "heavy-thunder-out") with { Volume = Volume, MaxInstances = 1 };
                ThunderLoopID = SoundEngine.PlaySound(ThunderLoopStyle);
                SoundPlayed = true;
            }
            else if (!SoundEngine.TryGetActiveSound(ThunderLoopID, out var ctiveSound) && SoundPlayed)
            {
                Projectile.Kill();
            }
            if (Projectile.timeLeft < 20)
            {
                isFadingOut = true;
            }

            if (!isFadingOut)
            {
                int frameSpeed = 4;

                Projectile.frameCounter++;

                if (Projectile.frameCounter >= frameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;

                    if (Projectile.frame >= 4)
                    {
                        Projectile.frame = 0;
                    }
                }
            }
            else
            {
                if (Projectile.frame < 4)
                {
                    Projectile.frame = 4;
                }

                int frameSpeed = 5;

                Projectile.frameCounter++;

                if (Projectile.frameCounter >= frameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerSummoner = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<CCShock>(), 600);
        }
        public override bool? CanDamage()
        {
            if (isFadingOut)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            return true;
        }
    }
}
