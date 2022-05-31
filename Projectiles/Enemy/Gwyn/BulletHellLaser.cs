using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy.Gwyn {
    class BulletHellLaserSpawner : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";

        public override void SetDefaults() {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.hostile = false;
        }

        internal float AI_Timer {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public override void AI() {
            AI_Timer++;
            if (AI_Timer == 1) {
                Main.NewText("here");
                Projectile.NewProjectile(new Vector2(Projectile.Center.X - Projectile.width, Projectile.Center.Y), new Vector2(0, 1), ModContent.ProjectileType<BulletHellLaser>(), Projectile.damage, 0);
            }
            Dust.NewDust(Projectile.Center, 1, 1, DustID.Clentaminator_Purple);
            if (AI_Timer > 300) {
                Projectile.Kill();
            }
        }
    }

    class BulletHellLaser : EnemyGenericLaser {

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;

            ProjectileSource = true;
            FollowHost = true;
            TelegraphTime = 150;
            FiringDuration = 370;
            MaxCharge = 150;
            LaserLength = (int)NPCs.Bosses.SuperHardMode.SoulOfCinder.ARENA_HEIGHT;
            TileCollide = false;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.BulletHellLaser;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSize = 4f;
        }

    }
}
