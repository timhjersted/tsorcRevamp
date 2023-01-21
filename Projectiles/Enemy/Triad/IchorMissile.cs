using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class IchorMissile : ModNPC
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Missile");
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.scale = 1.1f;
            NPC.noTileCollide = true;
            NPC.friendly = false;            
            NPC.noGravity = true;
            NPC.aiStyle = -1;
            NPC.lifeMax = 6;
            NPC.damage = 50;
            NPC.knockBackResist = 0;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        bool spawnedTrail = false;
        public override void AI()
        {
            if (!spawnedTrail)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.IchorMissileTrail>(), 0, 0, Main.myPlayer, 1, NPC.whoAmI);

                spawnedTrail = true;
            }

            NPC.rotation = NPC.velocity.ToRotation();
            Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ichor).noGravity = true;

            Player target = UsefulFunctions.GetClosestPlayer(NPC.Center);

            //Perform homing
            if (target != null)
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center, 1f, 12, target.velocity, false);
            }

            if (target == null || NPC.Distance(target.Center) < 50)
            {
                NPC.life = 0;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Ichor, 300);
        }
        public override void OnKill()
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 6f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 6f + 2f * MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 6f + -2f * MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 6f) * -10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 6f + MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 6f + -MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }
    }
}
