using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Prime
{
    class HomingMissile : ModNPC
    {

        public override void SetStaticDefaults()
        {
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
            NPC.timeLeft = 1200;

        }

        bool spawnedTrail = false;
        bool defused = false;
        int homingDelay = 0;
        public override void AI()
        {
            homingDelay++;
            if (!spawnedTrail)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<HomingMissileTrail>(), 0, 0, Main.myPlayer, 0, NPC.whoAmI);
                }
                spawnedTrail = true;
                if (NPC.ai[1] == 0)
                {
                    NPC.velocity = new Vector2(Main.rand.NextFloat(-5, 5), -10);
                }
                if (NPC.ai[1] == 1)
                {
                    NPC.velocity = Vector2.Zero;
                }
                if (NPC.ai[1] == 2)
                {
                    NPC.velocity = new Vector2(Main.rand.NextFloat(-35, 20), -15);
                }
            }

            NPC.rotation = NPC.velocity.ToRotation();
            Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ichor).noGravity = true;

            Player target = UsefulFunctions.GetClosestPlayer(NPC.Center);


            if (target != null)
            {

                if (NPC.ai[1] != 2 || target.Distance(NPC.Center) < 200)
                {
                    if (homingDelay > 30)
                    {
                        UsefulFunctions.SmoothHoming(NPC, target.Center, 0.3f, 5, target.velocity, false);
                    }
                }
                else
                {
                    if (NPC.velocity.Y < 7)
                    {
                        NPC.velocity.Y += 0.1f;
                    }
                    else
                    {
                        UsefulFunctions.SmoothHoming(NPC, target.Center, 0.01f, 5, target.velocity, false);
                    }
                    NPC.velocity.X *= 0.98f;
                }
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            NPC.life = 0;
            NPC.netUpdate = true;
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = NPC.Hitbox;
        }
        public override void OnKill()
        {
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.life = 0;
            NPC.netUpdate = true;
            UsefulFunctions.SimpleGore(NPC, "HomingMissile_DramaticCut1");
            UsefulFunctions.SimpleGore(NPC, "HomingMissile_DramaticCut2");
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC.life = 0;
            NPC.netUpdate = true;

            if (projectile.DamageType == DamageClass.Melee)
            {
                UsefulFunctions.SimpleGore(NPC, "HomingMissile_DramaticCut1");
                UsefulFunctions.SimpleGore(NPC, "HomingMissile_DramaticCut2");
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
            }
            else
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 500, 60);
                }
            }
        }

        public override void DrawBehind(int index)
        {
            base.DrawBehind(index);
        }

        float openingProgress;
        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (NPC.velocity.X < 0 && NPC.ai[0] != 2)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }
            //Vector2 offset = new Vector2(30, 0).RotatedBy(NPC.rotation);
            openingProgress += 0.34f;
            openingProgress = Math.Clamp(openingProgress, 0, 3);
            Rectangle sourceRectangle = new Rectangle(0, (int)openingProgress * (texture.Height / 4), texture.Width, texture.Height / 4);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture,
                NPC.Center - Main.screenPosition,
                sourceRectangle, Color.White, NPC.rotation + MathHelper.PiOver2, origin, NPC.scale, spriteEffects, 0);
            return false;
        }
    }
}
