using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class MarilithAura : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cataclysmic Firestorm");
        }

        public override void SetDefaults()
        {
            Projectile.width = 194;
            Projectile.height = 194;
            DrawOriginOffsetX = -96;
            DrawOriginOffsetY = 94;
            Main.projFrames[Projectile.type] = 7;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        Vector2 truePosition;
        Vector2 offset;
        Vector2 targetVector;
        float intro;
        int marilithDeadTimer;
        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>()))
            {
                marilithDeadTimer++;
                if(marilithDeadTimer > 45)
                {
                    Projectile.Kill();
                }
            }
            else
            {
                FireFiendMarilith marl = Main.npc[UsefulFunctions.GetFirstNPC(ModContent.NPCType<FireFiendMarilith>()).Value].ModNPC as FireFiendMarilith;
                truePosition = marl.NPC.Center;
                intro = marl.introTimer;
                targetVector = new Vector2(1, -0.2f);

                if (marl.MoveIndex == 2)
                {
                    Vector2 playerVector = UsefulFunctions.Aim(marl.NPC.Center, Main.LocalPlayer.Center, 2);
                    if (marl.MoveTimer < 120)
                    {
                        targetVector = Vector2.Lerp(targetVector, playerVector, marl.MoveTimer / 120f);
                    }
                    else if(marl.MoveTimer < 780)
                    {
                        targetVector = playerVector;
                    }
                    else
                    {
                        targetVector = Vector2.Lerp(playerVector, targetVector, (marl.MoveTimer - 780) / 120f);
                    }
                }
            }

            if (truePosition == Vector2.Zero)
            {
                truePosition = Projectile.Center;
            }
            Projectile.Center = Main.LocalPlayer.Center;

            Projectile.timeLeft = 2;
            //Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);
            
            
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //behindProjectiles.Add(index);
        }

        //Circular collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }

        public static ArmorShaderData data;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            
            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/MarilithFireAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "MarilithFireAuraPass");
            }

            //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);

            //Pass the flow directions parameter in through the "color" and "targetposition" variables, because there isn't a "direction" one
            data.UseColor(truePosition.X, truePosition.Y, offset.X);
            //data.UseTargetPosition(offset);
            data.UseOpacity(offset.Y);
            offset -= targetVector;
            //offset += new Vector2(-3f, 1.2f) / 300f;
            float saturation = intro / 120f;
            if(marilithDeadTimer > 0)
            {
                saturation = (45f - marilithDeadTimer) / 45f;
            }
            data.UseSaturation(saturation);
            //Apply the shader
            data.Apply(null);

            

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseTurbulent.Width, tsorcRevamp.NoiseTurbulent.Height);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, truePosition - Main.screenPosition - new Vector2(recsize.Width, recsize.Height) / 2 * 2.5f * 4f, recsize, Color.White, 0, Vector2.Zero, 2.5f * 4f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }
    }
}