using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Conqueror;

public class ConquerorGlobalProjectile : GlobalProjectile
{
    public bool AppliedConqueror;
    public override bool InstancePerEntity => true;
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player player = Main.player[projectile.owner];
        var modPlayer = player.GetModPlayer<ConquerorPlayer>();
        var tsorcPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.HasBuff(ModContent.BuffType<Conqueror>()) && ProjectileID.Sets.IsAWhip[projectile.type] && !AppliedConqueror)
            {
                if (modPlayer.Stacks < ConquerorPlayer.MaxStacks - 1 && !tsorcPlayer.WhipTipHit(projectile, projectile.WhipPointsForCollision, target.Hitbox))
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with 
                        { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.Stacks < ConquerorPlayer.MaxStacks - 2 && !tsorcPlayer.WhipTipHit(projectile, projectile.WhipPointsForCollision, target.Hitbox))
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with 
                        { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.008f }, player.Center);
                }
                else if (modPlayer.Stacks == ConquerorPlayer.MaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with 
                        { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                else if (modPlayer.Stacks == ConquerorPlayer.MaxStacks - 2 && tsorcPlayer.WhipTipHit(projectile, projectile.WhipPointsForCollision, target.Hitbox))
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with 
                        { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), ConquerorPlayer.Duration * 60);
                if (tsorcPlayer.WhipTipHit(projectile, projectile.WhipPointsForCollision, target.Hitbox))
                {
                    player.AddBuff(ModContent.BuffType<Conqueror>(), ConquerorPlayer.Duration * 60);
                }
                AppliedConqueror = true;
            }
            else if (projectile.DamageType != DamageClass.Summon && projectile.DamageType != DamageClass.SummonMeleeSpeed && modPlayer.Conqueror && tsorcRevamp.EnemiesOOA.Contains(target.type))
            {
                if (modPlayer.Stacks < ConquerorPlayer.MaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with 
                        { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.Stacks == ConquerorPlayer.MaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with 
                        { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), ConquerorPlayer.Duration * 60);
                AppliedConqueror = true;
            }
    }
}