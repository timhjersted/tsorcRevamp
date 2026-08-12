using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Runeterra;

public class FirstRunePrison : ModProjectile
{
    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 100;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        //Projectile.alpha = 160;
    }
    public override void AI()
    {
        NPC owner = Main.npc[(int)Projectile.ai[0]];
        Player target = Main.player[Projectile.owner];
        Projectile.velocity = target.velocity;
        target.AddBuff(ModContent.BuffType<RunePrisonDebuff>(), 2);
        if (owner.ai[0] == (float)RuneMage.ActionState.StartingFight)
        {
            Projectile.timeLeft = 2;
        }
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }
}