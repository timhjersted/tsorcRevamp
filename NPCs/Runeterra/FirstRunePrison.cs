using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Runeterra;

public class FirstRunePrison : ModProjectile
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(RunePrison));
    public const int Frames = 5;
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Frames;
        ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 104;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
    }
    public override void AI()
    {
        NPC owner = Main.npc[(int)Projectile.ai[0]];
        Player target = Main.player[Projectile.owner];
        Projectile.Center = target.Center;
        //target.AddBuff(ModContent.BuffType<RunePrisonDebuff>(), 2);
        
        if (owner.ai[0] == (float)RuneMage.ActionState.StartingFight && owner.active)
        {
            Projectile.timeLeft = 2;
        }
        
        UsefulFunctions.BasicAnimationLoop(Projectile, 10);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return UsefulFunctions.RenderTransparentTexture(Projectile, TransparentTextureHandler.TransparentTextureType.RunePrison, lightColor);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }
}