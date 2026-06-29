using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using UtfUnknown.Core.Models.SingleByte.Croatian;
using tsorcRevamp.Projectiles.Enemy.OolacileSorcerer;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class AbysmalOolacileSorcererTEST : ModNPC
    {
        int phase = 1;
        int attackTimer = 600;
        int attack = 1;
        bool fly = true;
        float angle = 0;

        bool wingDirection = true;
        float wingSpeed = 0;
        int wingTimer = 40;
        float wingAlpha = -2;
        bool drawWings = true;
        float wingRotation = MathHelper.ToRadians(-160);
        bool useAltWing = false;

        bool desparation = false;
        int desparationTimer = 1540;

        float velocityMulti = 1;

        Vector2 firstFloatDestination = new Vector2(500, 0);
        Vector2 secondFloatDestination = new Vector2(0, 500);
        Vector2 anchoredPos = new Vector2(0, 0);

        public override bool CheckDead()
        {
            if (!desparation || desparationTimer != 0)
            {
                desparation = true;
                NPC.netUpdate = true;
                NPC.life = 1;
                Main.NewText("The Grand Occultist of Oolacile is going mad!", Color.Red);
                useAltWing = true;
                return false;
            }

            return true;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float bossLifeScale, float balance)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.625f * balance);
            NPC.damage = (int)(NPC.damage * 0.6f);
        }

        public static void FloatAbovePlayer(Vector2 lookAtPosition, NPC NPC, float speed, float inertia, Vector2 destination, float radians, float fault)
        {
            if (NPC.WithinRange(destination, fault))
            {
                NPC.velocity *= 0.85f;
                return;
            }

            Vector2 toPlayer = lookAtPosition - NPC.Center;
            Vector2 toAbovePlayer = destination - NPC.Center;
            Vector2 toAbovePlayerNormalized = toAbovePlayer.SafeNormalize(Vector2.UnitY);

            Vector2 moveTo = toAbovePlayerNormalized * speed;
            NPC.velocity = (NPC.velocity * (inertia - 1) + moveTo) / inertia;
            if (lookAtPosition != Vector2.Zero)
            {
                NPC.rotation = toPlayer.ToRotation() + MathHelper.ToRadians(radians);
            }

        }
        public static void ShootProjectile(IEntitySource source, Vector2 targetPosition, float speed, int type, bool hasTileCollide, int count, float startAngle, float angleDecrement, Vector2 startPosition, float radius, int damage)
        {
            Vector2 distance = targetPosition - startPosition;
            Vector2 distanceNormalized = distance.SafeNormalize(Vector2.UnitX);
            float angle = startAngle;
            for (int i = 0; i < count; i++)
            {
                int projectile = Projectile.NewProjectile(source, startPosition, (distanceNormalized.RotatedBy(MathHelper.ToRadians(angle)) * speed).RotatedByRandom(MathHelper.ToRadians(radius)), type, damage, 1);

                Main.projectile[projectile].tileCollide = hasTileCollide;
                Main.projectile[projectile].friendly = false;
                Main.projectile[projectile].hostile = true;
                angle -= angleDecrement;
            }
        }
        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 56;
            NPC.damage = 100;
            NPC.defense = 75;
            NPC.lifeMax = 195000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 150000f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.boss = true;
            NPC.noTileCollide = true;
            Music = MusicID.Boss3;
            NPC.npcSlots = 1500f;
            NPC.scale = 1.2f;

            if (Main.getGoodWorld)
            {
                NPC.scale = 0.25f;
            }
        }

        private void ShootProjectileBetter(IEntitySource source, Vector2 targetPosition, float speed, int type, bool hasTileCollide, int count, float startAngle, float angleDecrement, Vector2 startPosition, float radius, int damage)
        {
            ShootProjectile(source, targetPosition, speed * velocityMulti, type, hasTileCollide, count, startAngle, angleDecrement, startPosition, radius, damage);
            if (Main.getGoodWorld)
            {
                ShootProjectile(source, targetPosition, speed * velocityMulti, type, hasTileCollide, count, startAngle, angleDecrement, startPosition, radius, damage);
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3;
        }
        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = 2 * frameHeight;
        }

        int spawnAnimationTimer = 300;
        int phase2Transition = 0;
        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            if (!useAltWing)
            {
                if (wingTimer > 10)
                {
                    wingSpeed += 1f * (wingDirection ? 1 : -1);
                }
                else
                {
                    wingSpeed *= 0.7f;
                }

                wingRotation += MathHelper.ToRadians(wingSpeed);

                wingTimer--;
                if (wingTimer == 0)
                {
                    wingTimer = 20;
                    wingDirection = !wingDirection;
                }
            }



            if (spawnAnimationTimer != 0)
            {
                NPC.defDamage = NPC.damage;
                NPC.damage = 0;
                spawnAnimationTimer--;

                if (spawnAnimationTimer == 299)
                {
                    NPC.Center = player.Center - new Vector2(0, 1500);
                    NPC.velocity.Y = 30;
                }

                NPC.velocity *= 0.975f;

                if (spawnAnimationTimer == 120)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath10 with { Pitch = -0.5f, Volume = 2 });
                }

                if (spawnAnimationTimer < 120)
                {
                    wingAlpha = MathHelper.Clamp(wingAlpha + 0.05f, 0, 1);
                }

                NPC.dontTakeDamage = true;

                if (spawnAnimationTimer == 0)
                {
                    NPC.dontTakeDamage = false;
                }
                return;
            }

            if (player.dead)
            {
                NPC.velocity.Y += 0.1f;
                NPC.EncourageDespawn(1);
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                return;
            }

            if (desparation)
            {
                NPC.dontTakeDamage = true;

                if (desparationTimer > 1200)
                {
                    if (desparationTimer == 1540)
                    {
                        NPC.rotation = 0;
                        NPC.velocity = new Vector2(0, -1);
                    }
                    else if (desparationTimer >= 1420)
                    {
                        NPC.velocity *= 1.05f;
                    }

                    else if (desparationTimer == 1419)
                    {
                        NPC.Center = player.Center - new Vector2(0, 1500);
                        NPC.velocity.Y = 30;
                    }
                    else if (desparationTimer > 1300)
                    {
                        NPC.velocity *= 0.974f;
                    }
                    else if (desparationTimer == 1300)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath10 with { Pitch = -0.5f, Volume = 2 });
                        NPC.rotation = -1;
                    }
                    else
                    {
                        NPC.rotation *= 1.04f;
                    }
                    desparationTimer--;
                    return;
                }

                FloatAbovePlayer(Vector2.Zero, NPC, 40f, 100, player.Center, 0, 20);

                NPC.rotation += 0.2f;

                int attackDelay = 10;
                if (desparationTimer % attackDelay == 0)
                {
                    ShootProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0, 1), 1, ModContent.ProjectileType<OolacileBolt>(), false, 3, 0, 0, NPC.Center, 360, 40);
                }

                desparationTimer--;

                if (desparationTimer == 0)
                {
                    NPC.life -= 1;
                    NPC.checkDead();
                }
                return;
            }

            if (Vector2.Distance(NPC.Center, player.Center) > 3000)
            {
                NPC.position += NPC.velocity;
                velocityMulti = 2;

                player.statLife -= 5;
            }
            else
            {
                velocityMulti = 1;
            }

            float lifeRatio = (float)NPC.life / (float)NPC.lifeMax;

            if (lifeRatio <= 0.5f && phase == 1)
            {
                attackTimer = 600;
                attack = 1;
                phase = 2;
                useAltWing = false;
                return;
            }
            if (phase == 1)
            {
                if (attack == 1)
                {
                    Vector2 flyDestination = (player.Center + new Vector2(0, -400)) - firstFloatDestination;
                    FloatAbovePlayer(NPC.Center + new Vector2(0, 1), NPC, 12.5f, 10, flyDestination, 0, 20);

                    if (NPC.WithinRange(new Vector2(flyDestination.X, NPC.Center.Y), 20))
                    {
                        firstFloatDestination.X *= -1;
                    }
                    int attackDelay = (int)MathHelper.Lerp(40, 25, 1 - lifeRatio);
                    if (attackTimer % attackDelay == 0)
                    {
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 5, ModContent.ProjectileType<OccultistStarBig>(), false, 1, 0, 0, NPC.Center, 0, 60);
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 1, ModContent.ProjectileType<OolacileBolt>(), false, 2, 22.5f, 45, NPC.Center, 0, 40);
                    }
                }


                else if (attack == 2)
                {


                    FloatAbovePlayer(NPC.Center + new Vector2(0, 1), NPC, 10, 100, player.Center, 0, 20);
                    useAltWing = true;

                    int attackDelay = (int)MathHelper.Lerp(60, 30, 1 - lifeRatio);
                    if (attackTimer % attackDelay == 0)
                    {
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center + (player.velocity * 30), 3, ModContent.ProjectileType<OccultistDarkOrb>(), false, 1, 0, 0, NPC.Center, 0, 60);
                    }
                }

                else if (attack == 3)
                {
                    Vector2 flyDestination = (player.Center + new Vector2(0, -500)) - (firstFloatDestination * 2);

                    int attackDelay = (int)MathHelper.Lerp(6, 4, 1 - lifeRatio);
                    if (fly)
                    {
                        FloatAbovePlayer(NPC.Center + new Vector2(0, 1), NPC, 20f, 10, flyDestination, 0, 20);
                    }
                    else if (attackTimer % attackDelay == 0)
                    {
                        ShootProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0, 1), 0.5f, ModContent.ProjectileType<OolacileBolt>(), false, 1, 0, 0, NPC.Center, 5, 40);
                    }

                    if (attackTimer % (attackDelay * 7) == 0)
                    {
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 0.5f, ModContent.ProjectileType<OolacileBolt>(), false, 1, 0, 0, player.Center + new Vector2(1000, Main.rand.Next(-700, 700)), 5, 40);
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 0.5f, ModContent.ProjectileType<OolacileBolt>(), false, 1, 0, 0, player.Center + new Vector2(-1000, Main.rand.Next(-700, 700)), 5, 40);
                    }

                    if (attackTimer % 200 == 50)
                    {
                        firstFloatDestination.X *= -1;
                        flyDestination = (player.Center + new Vector2(0, -400)) - (firstFloatDestination * 2);

                        Vector2 direction = (new Vector2(flyDestination.X, NPC.Center.Y) - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.velocity = new Vector2(30f, 30f) * direction;
                        SoundEngine.PlaySound(SoundID.Roar, NPC.position);

                        fly = false;
                        useAltWing = true;
                    }
                    else if (attackTimer % 200 == 150)
                    {
                        fly = true;
                        useAltWing = false;
                    }
                }
            }
            else
            {
                if (attack == 1)
                {
                    Vector2 flyDestination = (player.Center + firstFloatDestination) - secondFloatDestination;
                    FloatAbovePlayer(NPC.Center + new Vector2(0, 1), NPC, 17.5f, 10, flyDestination, 0, 20);

                    if (NPC.WithinRange(new Vector2(NPC.Center.X, flyDestination.Y), 20))
                    {
                        secondFloatDestination.Y *= -1;
                    }
                    int attackDelay = (int)MathHelper.Lerp(40, 25, 1 - lifeRatio);
                    if (attackTimer % attackDelay == 0)
                    {
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 5, ModContent.ProjectileType<OccultistStarBig>(), false, 1, 0, 0, NPC.Center, 0, 60);
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 1, ModContent.ProjectileType<OolacileBolt>(), false, 2, 22.5f, 45, NPC.Center, 0, 40);
                    }
                }

                if (attack == 2)
                {
                    Vector2 flyDestination = (player.Center + new Vector2(0, -400)) - (firstFloatDestination * 1.5f);
                    FloatAbovePlayer(NPC.Center + new Vector2(0, 1), NPC, 20f, 10, flyDestination, 0, 20);

                    if (NPC.WithinRange(new Vector2(flyDestination.X, NPC.Center.Y), 20))
                    {
                        firstFloatDestination.X *= -1;
                    }
                    if (NPC.Center.Y < (flyDestination.Y + 300))
                    {
                        int attackDelay = 5;
                        if (attackTimer % attackDelay == 0)
                        {
                            ShootProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0, 1), 1, ModContent.ProjectileType<OolacileBolt>(), false, 1, 0, 0, NPC.Center, 0, 40);
                        }
                    }

                    if (attackTimer % 6 == 0)
                    {
                        ShootProjectileBetter(NPC.GetSource_FromAI(), (anchoredPos + firstFloatDestination) + new Vector2(0, 1), 15, ModContent.ProjectileType<NonAccelDarkSlime>(), false, 1, 0, 0, (anchoredPos + firstFloatDestination), 0, 40);
                        ShootProjectileBetter(NPC.GetSource_FromAI(), (anchoredPos + -firstFloatDestination) + new Vector2(0, 1), 15, ModContent.ProjectileType<NonAccelDarkSlime>(), false, 1, 0, 0, (anchoredPos + -firstFloatDestination), 0, 40);
                    }

                }

                if (attack == 3)
                {
                    if (attackTimer > 540)
                    {

                    }
                    else
                    {
                        int attackDelay = (int)MathHelper.Lerp(80, 40, 1 - lifeRatio);
                        if (attackTimer % attackDelay == 0)
                        {
                            ShootProjectileBetter(NPC.GetSource_FromAI(), NPC.Center.RotatedByRandom(360), 5, ModContent.ProjectileType<NonAccelDarkSlime>(), false, 18, 0, 360 / 18, NPC.Center, 0, 40);
                        }

                        if (attackTimer % 2 == 0)
                        {
                            ShootProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, 5, ModContent.ProjectileType<NonAccelDarkSlime>(), false, 1, angle, 0, NPC.Center, 0, 40);
                        }

                        if (attackTimer % 40 == 0)
                        {
                            ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 5, ModContent.ProjectileType<NonAccelDarkSlime>(), false, 1, 0, 0, NPC.Center, 0, 40);
                        }

                        angle += Main.rand.Next(600, 1200) / 100;
                    }
                    fly = true;
                    NPC.velocity *= 0.9f;
                    useAltWing = true;
                }


                if (attack == 4)
                {
                    if (fly)
                    {
                        Vector2 flyDestination = (player.Center + player.velocity * 40) + new Vector2(0, -500);
                        FloatAbovePlayer(NPC.Center + new Vector2(0, 1), NPC, 20f, 10, flyDestination, 0, 20);
                    }

                    int attackDelay = 160;
                    if (attackTimer % attackDelay == attackDelay * 0.5f)
                    {
                        NPC.damage = NPC.defDamage;
                        fly = false;
                        useAltWing = true;
                        NPC.velocity = new Vector2(0, 30);
                        SoundEngine.PlaySound(SoundID.Roar, NPC.position);

                        Vector2 shootPos = new Vector2(NPC.Center.X - 2000, NPC.Center.Y - 500);
                        ShootProjectileBetter(NPC.GetSource_FromAI(), player.Center, 1, ModContent.ProjectileType<OccultistDarkOrb>(), false, 1, 0, 0, player.Center + new Vector2(0, -1000), 10, 40);

                        for (int i = 0; i < 40; i++)
                        {
                            ShootProjectileBetter(NPC.GetSource_FromAI(), shootPos + new Vector2(0, 1), 1, ModContent.ProjectileType<OolacileBolt>(), false, 1, 0, 0, shootPos, 0, 40);
                            shootPos.X += 120;
                        }
                    }

                    if (attackTimer % attackDelay == attackDelay * 0.3f)
                    {
                        NPC.damage = 0;
                        fly = true;
                        useAltWing = false;

                        ShootProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0, -1), 10, ModContent.ProjectileType<OolacileBolt>(), false, 5, 22.5f, 12.5f, NPC.Center, 0, 40);
                    }
                }
            }

            NPC.rotation = NPC.velocity.X * 0.01f;



            attackTimer--;
            if (attackTimer == 0)
            {
                NPC.damage = 0;
                attackTimer = 600;
                useAltWing = false;
                fly = true;
                anchoredPos = player.Center + new Vector2(0, -1000);
                attack++;

                int maxAttack = phase == 2 ? 5 : 4;

                if (attack == maxAttack)
                {
                    attack = 1;
                }
            }

        }
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (useAltWing)
            {
                modifiers.FinalDamage /= 2;
            }
        }
        private void DrawWings(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D wingLeftTexture = useAltWing ? ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkWing").Value : ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkWingAlt").Value;
            Texture2D wingRightTexture = wingLeftTexture;

            Vector2 leftWingOrigin = wingLeftTexture.Size() * (useAltWing ? new Vector2(0.7f, 0.2f) : new Vector2(1.1f, 0.8f));
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingRightTexture.Width - rightWingOrigin.X;

            spriteBatch.Draw(wingLeftTexture, NPC.Center - screenPos, null, drawColor * wingAlpha, useAltWing ? NPC.rotation : wingRotation, leftWingOrigin, 0.7f, SpriteEffects.None, 0f);
            spriteBatch.Draw(wingRightTexture, NPC.Center - screenPos, null, drawColor * wingAlpha, useAltWing ? NPC.rotation : -wingRotation, rightWingOrigin, 0.7f, SpriteEffects.FlipHorizontally, 0f);
        }
        public static Effect effect;
        float auraBonus = 1f;
        int expandRadius = 0;

        public static void RestartSpritebatch(ref SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            auraBonus *= 0.9f;
            auraBonus += 0.1f;

            Color rgbColor = new Color(130, 8, 30);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            float colorIntensity = 0.5f + auraBonus * 0.2f;
            int auraSize = 200 + expandRadius / 4 + 30;

            if (useAltWing)
            {
                colorIntensity *= 2f;
                auraSize = (int)(0.8f * auraSize);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, auraSize, auraSize);
            Vector2 origin = sourceRectangle.Size() / 2f;

            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseVoronoi.Width);
            //replace this tsorcRevamp.NoiseVoronoi

            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4() * colorIntensity * 1.2f);
            effect.Parameters["ringProgress"].SetValue(0.6f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.2f);
            effect.Parameters["scaleFactor"].SetValue(3.5f);

            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseVoronoi, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale * 1.1f, SpriteEffects.None, 0);
            //replace this tsorcRevamp.NoiseVoronoi

            RestartSpritebatch(ref Main.spriteBatch);
            //replace this with UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);


            if (wingAlpha <= 0)
            {
                return true;
            }
            if (useAltWing)
            {
                Texture2D texture = TextureAssets.Npc[NPC.type].Value;

                Rectangle sourceRect = NPC.frame;

                Vector2 origin2 = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);

                Vector2 pos = NPC.Center - screenPos;


                spriteBatch.Draw(
                    texture,
                    pos,
                    sourceRect,
                    drawColor,
                    NPC.rotation,
                    origin2,
                    NPC.scale,
                    SpriteEffects.None,
                    0f
                );
            }

            DrawWings(spriteBatch, screenPos, drawColor);
            return !useAltWing;
        }
    }

    public class NonAccelDarkSlime : OccultistStar
    {
        public override string Texture => ModContent.GetInstance<OolacileBolt>().Texture;
        public override void SetDefaults()
        {
            accel = false;
            base.SetDefaults();
        }
    }
}
