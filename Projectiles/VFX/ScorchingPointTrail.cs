using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    class ScorchingPointTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Scorching Trail");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 999999999;
            Projectile.penetrate = -1;
            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 111;
            Projectile.hide = true;
            collisionPadding = 50;
            NPCSource = false;

            trailCollision = true;
            collisionFrequency = 5;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 35;
        }

    }
}