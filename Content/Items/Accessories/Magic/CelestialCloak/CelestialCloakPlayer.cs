using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.Content.Items.Accessories.Magic.CelestialCloak;

public class CelestialCloakPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void PostUpdateMiscEffects()
    {
        if (Equipped)
        {
            int trueMaxMana = (int)(Player.statManaMax2 / (Player.HasBuff(ModContent.BuffType<ArcaneSorcery>()) 
                ? (100f + ArcaneSorceryPlayer.MaxManaAmplifier) / 100f : 1f));
            Player.thorns += 0.1f + (trueMaxMana / 50f);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped && (hit.DamageType == DamageClass.Magic || hit.DamageType == DamageClass.MagicSummonHybrid))
        {
            int trueMaxMana = (int)(Player.statManaMax2 / (Player.HasBuff(ModContent.BuffType<ArcaneSorcery>()) 
                ? (100f + ArcaneSorceryPlayer.MaxManaAmplifier) / 100f : 1f));
            if (Main.rand.NextBool(25))
            {
                Vector2 starvector1 = new Vector2(-40, -200) + target.Center;
                Vector2 starvector2 = new Vector2(40, -200) + target.Center;
                Vector2 starvector3 = new Vector2(0, -200) + target.Center;
                Vector2 starmove1 = new Vector2(+4, 20);
                Vector2 starmove2 = new Vector2(-4, 20);
                Vector2 starmove3 = new Vector2(0, 20);
                if (Main.myPlayer == Player.whoAmI)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector1, starmove1, ProjectileID.ManaCloakStar, trueMaxMana / 5, 2f, Main.myPlayer);
                    Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector2, starmove2, ProjectileID.ManaCloakStar, trueMaxMana / 5, 2f, Main.myPlayer);
                    Projectile.NewProjectileDirect(Projectile.GetSource_NaturalSpawn(), starvector3, starmove3, ProjectileID.ManaCloakStar, trueMaxMana / 5, 2f, Main.myPlayer);
                }
            }
        }
    }
}