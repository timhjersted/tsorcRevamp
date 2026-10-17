using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Ranged.KrakenSet;

public class KrakenSetProj : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public bool Empowered;
    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Empowered)
        {
            modifiers.SourceDamage += KrakenCarcass.TsunamiDmgBoost / 100f;
        }
    }
}