using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Sentry
{
    public abstract class FloatingSentryProjectile : ModProjectile
    {
        public abstract int ShotCooldown { get; } //the cooldown between each shot, 60 is equal to 1 second unless you give the projectile extra updates
        public abstract int SentryShotCooldownReductionOnSpawn { get; } //all sentries of this type get to shoot upon being spawned immediately, set this to shotcooldown in the sentry if you don't want the sentry to be able to shoot almost instantly after being spawned
        public abstract int ProjectileFrameCount { get; } //may have up to 6 frames, you can extend this by extending the switch case below
        public abstract int ProjectileWidth { get; }
        public abstract int ProjectileHeight { get; }
        public abstract DamageClass ProjectileDamageType { get; } //should be SUmmon usually but I made an exception for one
        public abstract bool ContactDamage {  get; } //if it can deal damage on contact
        public abstract int ShotProjectileType { get; }
        public abstract float ProjectileInitialVelocity { get; } 
        public abstract int AI1 { get; } //passes this value into projectile.ai[1] of the shot projectile
        public abstract int AI2 { get; } //same as above except it's projectile.ai[2]
        public abstract bool PlaysSoundOnShot { get; } //if it plays a sound each time ti shoots a projectile
        public abstract SoundStyle ShootSoundStyle { get; }
        public abstract float ShootSoundVolume { get; }
        public abstract bool SpawnsDust { get; } //if the sentry should spawn dust around itself
        public abstract int ProjectileDustID { get; }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = ProjectileFrameCount;
            ProjectileID.Sets.TurretFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = ProjectileWidth;
            Projectile.height = ProjectileHeight;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = ProjectileDamageType;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Projectile.SentryLifeTime;

            Projectile.sentry = true;
            CustomSetDefaults(); //need to actually call the custom function at the point it's usually called at
        }

        public virtual void CustomSetDefaults()
        {
            //if you need to set anything else in SetDefaults for your specific sentry, do it here
            //if you need to add some other function in a different hook, you can add a custom virtual void like this too
            //ideally you should just make the sentries projectile do the custom stuff if possible
        }

        public override bool? CanDamage()
        {
            return ContactDamage;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            Projectile.ai[0]++;

            if (Projectile.ai[0] >= ShotCooldown)
            {
                Projectile SentryShot = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center, Projectile.DirectionTo(Main.MouseWorld) * ProjectileInitialVelocity, ShotProjectileType, Projectile.damage, Projectile.knockBack, owner.whoAmI, ShotCooldown - SentryShotCooldownReductionOnSpawn, AI1, AI2);
                SentryShot.CritChance = Projectile.CritChance;
                Projectile.ai[0] = 0;
            }

            switch (Projectile.ai[0])
            {
                case float FirstFrame when (FirstFrame >= 0 && FirstFrame <= ShotCooldown / Main.projFrames[Type]):
                    {
                        Projectile.frame = 0; //first frame
                        break;
                    }
                case float SecondFrame when (SecondFrame > ShotCooldown / Main.projFrames[Type] && SecondFrame <= ShotCooldown / Main.projFrames[Type] * 2):
                    {
                        Projectile.frame = 1; //second frame
                        break;
                    }
                case float ThirdFrame when ThirdFrame > ShotCooldown / Main.projFrames[Type] * 2 && ThirdFrame <= ShotCooldown / Main.projFrames[Type] * 3:
                    {
                        Projectile.frame = 2; //and so on
                        break;
                    }
                case float FourthFrame when FourthFrame > ShotCooldown / Main.projFrames[Type] * 3 && FourthFrame <= ShotCooldown / Main.projFrames[Type] * 4:
                    {
                        Projectile.frame = 3;
                        break;
                    }
                case float FifthFrame when FifthFrame > ShotCooldown / Main.projFrames[Type] * 4 && FifthFrame <= ShotCooldown / Main.projFrames[Type] * 5:
                    {
                        Projectile.frame = 4;
                        break;
                    }
                case float SixthFrame when SixthFrame > ShotCooldown / Main.projFrames[Type] * 5 && SixthFrame <= ShotCooldown / Main.projFrames[Type] * 6:
                    {
                        Projectile.frame = 5; 
                        break;
                    }
                case float SeventhFrame when SeventhFrame > ShotCooldown / Main.projFrames[Type] * 6 && SeventhFrame <= ShotCooldown / Main.projFrames[Type] * 7:
                    {
                        Projectile.frame = 6; 
                        break;
                    }
                case float EighthFrame when EighthFrame > ShotCooldown / Main.projFrames[Type] * 7 && EighthFrame <= ShotCooldown / Main.projFrames[Type] * 8:
                    {
                        Projectile.frame = 7; 
                        break;
                    }
                case float NinthFrame when NinthFrame > ShotCooldown / Main.projFrames[Type] * 8 && NinthFrame <= ShotCooldown / Main.projFrames[Type] * 9:
                    {
                        Projectile.frame = 8; 
                        break;
                    }
                case float TenthFrame when TenthFrame > ShotCooldown / Main.projFrames[Type] * 9: //think about how to extend this
                    {
                        Projectile.frame = 9; //tenth frame
                        break;
                    }
            }

            if (Projectile.ai[0] == 0 && PlaysSoundOnShot)
            {
                SoundEngine.PlaySound(ShootSoundStyle with { Volume = ShootSoundVolume }, Projectile.Center);
            }

            if (SpawnsDust)
            {
                Dust.NewDust(Projectile.VisualPosition, Projectile.width, Projectile.height, ProjectileDustID);
            }

            CustomAI();
        }

        public virtual void CustomAI() { }
    }
}