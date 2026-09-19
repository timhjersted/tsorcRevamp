using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Magic.GreatSoulArrowStaff;

public class GreatSoulArrowStaffNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool IsSoulstruck;
    public override void ResetEffects(NPC npc)
    {
        IsSoulstruck  = false;
    }
    public float CheckSoulstruckAmplifier()
    {
        return (IsSoulstruck ? (1f + (GreatSoulArrowStaffItem.SoulAmplifier / 100f)) : 1f);
    }
    public override void DrawEffects(NPC npc, ref Color drawColor)
    {
        if (IsSoulstruck)
        {
            Lighting.AddLight(npc.Center, .4f, .4f, .850f);

            if (Main.rand.NextBool(6))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 68, 0, 0, 30, default(Color), 1.25f);
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity += npc.velocity;
            }
        }
    }
}