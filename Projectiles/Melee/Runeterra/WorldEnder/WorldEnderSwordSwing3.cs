using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.UI;

namespace tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder
{
    public class WorldEnderSwordSwing3 : WorldEnderSwordSwingBase
    {
        public override int Frames => 12;
        public override int Width => 208;
        public override int Height => 208;
        public override int Tier => 3;
        public override void CustomAI(Player player)
        {
            HitboxSize = new Vector2(80, 80);
            Hitbox1 = player.Center + (Velocity * 1f).RotatedBy(0);
            Hitbox2 = player.Center + (Velocity * 1.35f).RotatedBy(0.55);
            Hitbox3 = player.Center + (Velocity * 1.6f).RotatedBy(0.25);
            Hitbox4 = player.Center + (Velocity * 1.6f).RotatedBy(-0.1);
            Hitbox5 = player.Center + (Velocity * 1.4f).RotatedBy(-0.4);
            Hitbox6 = player.Center + (Velocity * 0.8f).RotatedBy(-0.65);
            Hitbox7 = player.Center + (Velocity * 0.4f).RotatedBy(0);
            Hitbox8 = player.Center + (Velocity * 0.75f).RotatedBy(0.8);
            /*
            Dust.NewDustPerfect(Hitbox1, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox2, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox3, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox4, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox5, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox6, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox7, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox8, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            */
            CritHitboxSize = new Vector2(100, 100);
            CritHitbox1 = player.Center + (Velocity * 1f).RotatedBy(0);
            //Dust.NewDustPerfect(CritHitbox1, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
        }
    }
}
