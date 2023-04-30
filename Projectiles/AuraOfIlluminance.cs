using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class AuraOfIlluminance : Projectiles.VFX.DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Illuminant Trail");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = NPC.immuneTime;

            trailWidth = 45;
            trailPointLimit = 2000;
            trailMaxLength = 2000; 
            trailCollision = true;
            collisionFrequency = 3;
            noFadeOut = false;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void AI()
        {
            base.AI();
            float totalDisplacement = 0;
            float heightDisplacement = 0;
            float widthDisplacement = 0;
            bool intersection = false;
            Vector2 averageCenter = Vector2.Zero;
            for (int i = 0; i < trailPositions.Count; i++)
            {
                if (!intersection && i < (trailPositions.Count * 5) / 6 && (trailPositions[trailPositions.Count - 1] - trailPositions[i]).LengthSquared() < 1000)
                {
                    intersection = true;
                }
                averageCenter += trailPositions[i] / trailPositions.Count;
            }
            for (int i = 0; i < trailPositions.Count; i++)
            {
                totalDisplacement += (trailPositions[i] - averageCenter).LengthSquared();
                widthDisplacement += Math.Abs(trailPositions[i].X - averageCenter.X);
                heightDisplacement += Math.Abs(trailPositions[i].Y - averageCenter.Y);
            }

            //Main.NewText(totalDisplacement + " " + widthDisplacement + " " + heightDisplacement);

            if(trailCurrentLength > 1900 && totalDisplacement > 8000000 && intersection && widthDisplacement > 10000 && heightDisplacement > 10000)
            {
                Projectile.Kill();
                if (Main.rand.NextBool(20))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Coins with { Volume = 2f }, averageCenter);
                    for (int i = 0; i < 120; i++)
                    {
                        if (Main.rand.NextBool(4))
                        {
                            if (Main.rand.NextBool(4))
                            {
                                Item.NewItem(Projectile.GetSource_FromThis(), averageCenter + Main.rand.NextVector2CircularEdge(300, 300), Vector2.Zero, ItemID.GoldCoin);
                            }
                            else
                            {
                                Item.NewItem(Projectile.GetSource_FromThis(), averageCenter + Main.rand.NextVector2CircularEdge(300, 300), Vector2.Zero, ItemID.SilverCoin);
                            }
                        }
                        else
                        {
                            Item.NewItem(Projectile.GetSource_FromThis(), averageCenter + Main.rand.NextVector2CircularEdge(300, 300), Vector2.Zero, ItemID.CopperCoin);
                        }
                    }
                }
                else
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 2f }, averageCenter);
                }
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), averageCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.TriadDeath>(), 10, 0, Main.myPlayer, 0);
                for(int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC thisNPC = Main.npc[i];
                    if(thisNPC.active && !thisNPC.friendly && thisNPC.Distance(averageCenter) < 1000 && thisNPC.damage > 0 && !thisNPC.dontTakeDamage)
                    {
                        float damage = 1 - (thisNPC.Distance(averageCenter) / 1000f);
                        damage *= damage;
                        if (damage > 0)
                        {
                            Main.npc[i].StrikeNPC((int)(damage * 2000), 0, 0);
                        }
                    }
                }
            }
        }

        public override bool HostEntityValid()
        {
            if (Main.player[(int)Projectile.ai[0]].active && Main.player[(int)Projectile.ai[0]].GetModPlayer<tsorcRevampPlayer>().AuraOfIlluminance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Entity HostEntity => Main.player[(int)Projectile.ai[0]];

        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            collisionEndPadding = trailPositions.Count / 24;
            trailWidth = 40;

            collisionEndPadding = 0;
            collisionPadding = 0;

            Color shaderColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, (float)Main.timeForVisualEffects, 0.03f);


            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["finalStand"].SetValue(0);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["shaderColor2"].SetValue(new Color(0.2f, 0.7f, 1f).ToVector4());
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

    }
}