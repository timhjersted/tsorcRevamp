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
    class IchorGlob : ModNPC
    {
        
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ichor Glob");
        }

        public override void SetDefaults()
        {
            NPC.width = 60;
            NPC.height = 60;
            NPC.noTileCollide = true;
            NPC.friendly = false;
            NPC.noGravity = true;
            NPC.aiStyle = -1;
            NPC.lifeMax = 6;
            NPC.damage = 50;
            NPC.knockBackResist = 0;
            NPC.scale = 1;

        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        bool spawnedTrail = false;
        public override void AI()
        {
            NPC.width = 60;
            NPC.height = 60;
            if (!spawnedTrail)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.IchorTrail>(), 0, 0, Main.myPlayer, 0, NPC.whoAmI);
                }
                spawnedTrail = true;
            }

            NPC.rotation = NPC.velocity.ToRotation();
            Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ichor).noGravity = true;

            Player target = UsefulFunctions.GetClosestPlayer(NPC.Center);

            if (target != null)
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center, 1f, 12, target.velocity, false);
            }
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            NPC.life = 0;
            NPC.netUpdate = true;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity) * -10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            target.AddBuff(BuffID.Ichor, 300);
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = NPC.Hitbox;
        }
        public override void OnKill()
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity) * -10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-MathHelper.Pi / 3f) * 10, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), NPC.damage / 4, 1, Main.myPlayer);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }
    }
}
