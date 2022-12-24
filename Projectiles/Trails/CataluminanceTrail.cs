using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Trails
{
    class CataluminanceTrail : DynamicTrail
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
            Projectile.hostile = true;
            Projectile.friendly = false;
            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 9999999;
            Projectile.hide = true;
            collisionPadding = 50;
           
            trailCollision = true;
            collisionFrequency = 5;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float timer = 0;
        float transitionTimer = 0;
        public override void AI()
        {
            if (hostNPC != null && hostNPC.active && hostNPC.life < hostNPC.lifeMax / 2f)
            {
                transitionTimer++;
            }

            timer++;

            //A phase is 900 seconds long
            //Once that is over, stop adding new positions
            if (timer < 899)
            {
                base.AI();
            }

            //Once the boss is all the way back to that stage again, then start removing the old positions
            if (timer > 2700 || !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Cataluminance>()))
            {
                if (trailPositions.Count > 3)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 35;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        bool pinkTrail = false;
        Color trailColor = new Color(0.2f, 0.7f, 1f);
        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            collisionPadding = 8;
            trailWidth = 100;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            //I do it like this so it retains its color state even if the host NPC dies or despawns
            if (hostNPC != null && hostNPC.active && hostNPC.life < hostNPC.lifeMax / 2f && transitionTimer <= 120)
            {
                trailColor = Color.Lerp(new Color(0.2f, 0.7f, 1f), new Color(1f, 0.7f, 0.85f), transitionTimer / 120);
            }

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(trailColor.ToVector4());
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}