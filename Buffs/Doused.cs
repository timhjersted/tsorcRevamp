using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    // "Doused" — the Quara Pincher's black ichor. Applied by ichor globs / ignited patches. It SLOWS,
    // it BLEEDS (no natural regen), and — the signature — it PRIMES the target: if a doused player is set
    // on fire (the Pincher's Ember Lob, or standing in an ignited ichor patch), the ichor detonates for a
    // burst of explosive fire damage and the coating is spent.
    //
    // (Vanilla BuffID.Oiled has NO player-side effect — it only makes NPCs take more fire damage — so this
    // custom buff is the only way to do the "prime for explosive fire" mechanic. See memory
    // project_quara_pincher_rework.) Placeholder icon = the vanilla Oiled buff sprite; real art comes later.
    public class Doused : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Oiled;

        public const int DetonationDamage = 45;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 0.75f; //slow
            player.bleed = true;       //bleeding: prevents natural life regen

            //dripping black ichor
            if (Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(player.position, player.width, player.height, DustID.Ichor, 0f, 0.6f, 150, default, 0.7f);
                Main.dust[d].velocity *= 0.2f;
            }

            //Explosive prime: if the doused player is ignited, detonate once and consume the coating.
            bool ignited = player.HasBuff(BuffID.OnFire) || player.HasBuff(BuffID.OnFire3) || player.onFire || player.onFire3;
            if (ignited && player.whoAmI == Main.myPlayer)
            {
                Detonate(player);
                player.ClearBuff(Type);
            }
        }

        static void Detonate(Player player)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.1f }, player.Center);
            UsefulFunctions.ScreenShake(player.Center, 4f, 16, 6f, 500f);
            for (int i = 0; i < 34; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                int d = Dust.NewDust(player.position, player.width, player.height, DustID.Torch, vel.X, vel.Y, 60, default, 1.6f);
                Main.dust[d].noGravity = true;
            }
            player.AddBuff(BuffID.OnFire3, 3 * 60);
            player.Hurt(PlayerDeathReason.ByCustomReason(player.name + " was consumed by ichor flame."), DetonationDamage, 0);
        }
    }
}
