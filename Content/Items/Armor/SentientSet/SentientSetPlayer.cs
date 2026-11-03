using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.SentientSet;

public class SentientSetPlayer : ModPlayer
{
    public bool SetBonus;
    public SentientHead EquippedHead;
    public SentientTorso EquippedTorso;
    public SentientLegs EquippedLegs;
    public override void ResetEffects()
    {
        SetBonus = false;
        
        EquippedHead = null;
        EquippedTorso = null;
        EquippedLegs = null;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (EquippedHead != null && target.type != NPCID.TargetDummy)
        {
            EquippedHead.DamageDealt += damageDone;
        }
    }
    public override void OnHurt(Player.HurtInfo info)
    {
        if (EquippedTorso != null)
        {
            EquippedTorso.DamageTaken += info.SourceDamage;
        }
    }
    public Vector2 LastPosition;
    public override void PostUpdateEquips()
    {
        if (EquippedLegs != null)
        {
            LastPosition = Player.Center;
        }
    }
    public override void PostUpdate()
    {
        if (EquippedLegs != null)
        {
            EquippedLegs.DistanceTraveled += Vector2.Distance(Player.Center, LastPosition);
        }
    }
}