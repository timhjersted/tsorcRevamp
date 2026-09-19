using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;

namespace tsorcRevamp.Content.Items.Accessories.Summon.Goredrinker
{
    public class GoredrinkerPlayer : ModPlayer
    {
        public bool Equipped;
        public override void ResetEffects()
        {
            Equipped = false;
        }

        public bool Ready;
        public bool Swung;
        public int Hits;
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[proj.owner];
            if (Equipped && proj.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[proj.type] && !player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && Swung)
            {
                int heal = (int)MathF.Max(
                    MathF.Min(
                        (Player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(GoredrinkerItem.HealBaseValue) *
                            Player.statLifeMax2 / Player.statLife), 20) / (int)((float)Hits * 1.5f + 1), 1);
                Player.statLife += heal;
                Player.HealEffect(heal);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GoredrinkerHit") with { Volume = 0.25f }, target.Center);
                Hits++;
            }
            else if (Equipped && proj.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[proj.type] && player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()))
            {
                int buffIndex = 0;
                foreach (int buffType in player.buffType)
                {
                    if (buffType == ModContent.BuffType<GoredrinkerCooldown>())
                    {
                        if (Player.buffTime[buffIndex] < 15)
                        {
                            Hits = 0;
                        }
                        Player.buffTime[buffIndex] -= 15;
                    }
                    buffIndex++;
                }
            }
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Equipped && proj.DamageType == DamageClass.SummonMeleeSpeed && !Player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && Swung && ProjectileID.Sets.IsAWhip[proj.type])
            {
                modifiers.SourceDamage += GoredrinkerItem.WhipDmgRange / 100f / 3f;
            }
        }
    }
}

