using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class DarkUltimaWeaponDummyProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.aiStyle = -1;
            projectile.width = 30;
            projectile.height = 30;
            projectile.hostile = true;
            projectile.penetrate = 9999;
            projectile.melee = true;
            projectile.tileCollide = false;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Ultima Weapon");
        }
        public NPC Sword
        {
            get => Main.npc[(int)projectile.ai[0]];
            set => Main.npc[(int)projectile.ai[0]] = value;
        }
        public NPC DarkCloud
        {
            get => Main.npc[(int)Sword.ai[0]];
            set => Main.npc[(int)Sword.ai[0]] = value;
        }
        public Vector2 SwordCenter
        {
            //Compensating for the sword's janky as hell hitbox.
            get => Sword.Center + new Vector2(0, -62);
        }

        public override void AI()
        {            
            if (Sword == null || Sword.active == false || DarkCloud == null || DarkCloud.active == false)
            {
                projectile.Kill();
            }

            if (projectile.ai[1] == 0)
            {
                projectile.Center = SwordCenter + new Vector2(16, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
            }
            if (projectile.ai[1] == 1)
            {
                projectile.Center = SwordCenter + new Vector2(32, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
            }
            if (projectile.ai[1] == 2)
            {
                projectile.Center = SwordCenter + new Vector2(48, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
            }
            else if (projectile.ai[1] == 3)
            {
                projectile.Center = SwordCenter + new Vector2(64, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
            }
            else if (projectile.ai[1] == 4)
            {
                projectile.Center = SwordCenter;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            return false;
        }
    }
}