using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Runeterra;

public class RunePrison : ModProjectile
{
    public const int Frames = 5;
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Frames;
    }

    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 104;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 300;
    }
    public override void AI()
    {
        Player target = Main.player[Projectile.owner];
        Projectile.Center = target.Center;
        target.AddBuff(ModContent.BuffType<RunePrisonDebuff>(), 2);
        
        UsefulFunctions.BasicAnimationLoop(Projectile, 10);
    }
    
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        return UsefulFunctions.RenderTransparentTexture(Projectile, TransparentTextureHandler.TransparentTextureType.RunePrison, lightColor);
    }
}