using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class CrimsonDrain : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
        }
    }

    // Ring dust for the Crimson Drain aura. Vanilla LifeDrain dust (235) hard-codes an opaque additive colour in GetAlpha and
    // ignores dust.alpha, so this copies its motion (UpdateType) and sprite (vanilla dust sheet frame) and draws it at Opacity.
    public class CrimsonDrainDust : ModDust
    {
        public const float Opacity = 0.5f;

        public override string Texture => "Terraria/Images/Dust";

        public override void SetStaticDefaults()
        {
            UpdateType = DustID.LifeDrain;
        }

        // Vanilla sheet layout: 10px columns by type % 100, 30px rows by type / 100. tML has already rolled one of the 3
        // variants into frame.Y (0/10/20), so offset to the LifeDrain cell.
        public override void OnSpawn(Dust dust)
        {
            dust.frame.X = DustID.LifeDrain % 100 * 10;
            dust.frame.Y += DustID.LifeDrain / 100 * 30;
        }

        // Same white-additive colour vanilla uses for 235 (A = 0), scaled down so the ring reads as translucent.
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return new Color(255, 255, 255, 0) * Opacity;
        }
    }
}
