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
            projectile.aiStyle = 0;
            projectile.height = 16;
            projectile.scale = 1.2f;
            projectile.tileCollide = false;
            projectile.width = 16;
            projectile.hostile = false;
        }

        internal float AI_Timer {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }
        public override void AI() {
            AI_Timer++;
            if (AI_Timer == 1) {
                Main.NewText("here");
                Projectile.NewProjectile(new Vector2(projectile.Center.X - projectile.width, projectile.Center.Y), new Vector2(0, 1), ModContent.ProjectileType<BulletHellLaser>(), projectile.damage, 0);
            }
            Dust.NewDust(projectile.Center, 1, 1, DustID.Clentaminator_Purple);
            if (AI_Timer > 300) {
                projectile.Kill();
            }
        }
    }

    class BulletHellLaser : EnemyGenericLaser {

        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.hide = true;

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
