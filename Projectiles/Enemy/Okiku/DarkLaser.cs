using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{

    public class DarkLaser : GenericLaser
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 999;

            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };

            LaserDebuffs = new List<int>();
            DebuffTimers = new List<int>();

            CastLight = false;

            Additive = true;

            ProjectileSource = true;
            FollowHost = true;

            LaserName = "Dark Laser";
            TelegraphTime = 300;
            MaxCharge = 240;
            FiringDuration = 940;
            LaserLength = 10000;
            LaserColor = Color.Purple;
            TileCollide = false;
            CastLight = true;
            LaserDebuffs.Add(ModContent.BuffType<Buffs.Debuffs.DarkInferno>());
            DebuffTimers.Add(100);
        }

        float rotationSpeed = 0.01f;
        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()))
            {
                Projectile.Kill();
            }
            if (Projectile.ai[0] >= 10)
            {
                Projectile.timeLeft = 9999;
                FiringDuration = 99999;
                Projectile.ai[0] -= 10;
                rotationSpeed = 0.006f;
            }
            Projectile.rotation += rotationSpeed;
            Projectile.velocity = (Projectile.rotation + MathHelper.TwoPi * Projectile.ai[0] / 5f).ToRotationVector2();
            base.AI();
        }
    }
}
