using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfSpirituality;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra;

public class RuneterraOrbPlayer : ModPlayer
{
    public int EssenceThief = 0;
    public const int MaxSpiritRushCharges = 3;
    public int SpiritRushCharges = MaxSpiritRushCharges;
    public float SpiritRushTimer;
    public int SpiritRushSoundStyle;
    public int SpiritRushCooldown;
    public const int SpiritRushCooldownTime = 60;
    public Vector2 SpiritRushVelocity;

    public override void PreUpdateMovement()
    {
        if (SpiritRushTimer > 0)
        {
            Player.velocity = SpiritRushVelocity;
            Player.RefreshMovementAbilities();
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.specialAbility.JustPressed)
        {
            int dashManaCost = Player.GetManaCost(Player.HeldItem) * OrbOfSpirituality.DashManaCostMultiplier;
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<ThrownOrbOfSpirituality>()] != 0)
            {
                dashManaCost = (int)(dashManaCost / (1f - RuneterraOrb.FlameManaCostReduction / 100f)); //item mana cost gets halved while orb is out so this is needed to correct that
            }
            if (Player.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>() 
                && Player.statMana >= dashManaCost 
                && !Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>())
                && SpiritRushCharges == MaxSpiritRushCharges)
            {
                Player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDash>(), OrbOfSpirituality.DashBuffDuration * 60);
                Player.statMana -= dashManaCost;
            }
            if (Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()) && SpiritRushCooldown <= 0 && SpiritRushCharges > 0)
            {
                Player.immune = true;
                SpiritRushVelocity = Player.DirectionTo(Main.MouseWorld) * 25f;
                SpiritRushTimer = (int)(60 * 0.3f);
                SpiritRushCooldown = SpiritRushCooldownTime;
                Player.SetImmuneTimeForAllTypes(60);
                if (SpiritRushSoundStyle == 0)
                {
                    SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_Dash1") with { Volume = RuneterraOrb.OrbSoundVolume });
                    SpiritRushSoundStyle += 1;
                }
                else
                if (SpiritRushSoundStyle == 1)
                {
                    SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_Dash2") with { Volume = RuneterraOrb.OrbSoundVolume });
                    SpiritRushSoundStyle += 1;
                }
                else
                if (SpiritRushSoundStyle == 2)
                {
                    SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_Dash3") with { Volume = RuneterraOrb.OrbSoundVolume });
                    SpiritRushSoundStyle = 0;
                }
                Projectile.NewProjectile(Projectile.GetSource_None(), Player.Center, Vector2.One, ModContent.ProjectileType<FlameSpiritRush>(), Player.HeldItem.damage, Player.HeldItem.knockBack, Player.whoAmI, 1);
                SpiritRushCharges--;
            }
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.active && !target.friendly && Main.rand.NextBool((int)(100f / OrbOfDeception.EssenceThiefOnKillChance)) && target.life <= 0)
        {
            if (Player.HeldItem.type == ModContent.ItemType<OrbOfDeception>())
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, Vector2.Zero, ModContent.ProjectileType<EssenceThiefDelivery>(), 0, 0, Player.whoAmI, 0, 1);
            }
            else if (Player.HeldItem.type == ModContent.ItemType<OrbOfFlame>())
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, Vector2.Zero, ModContent.ProjectileType<EssenceThiefDelivery>(), 0, 0, Player.whoAmI, 1, 1);
            }
            else if (Player.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>())
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, Vector2.Zero, ModContent.ProjectileType<EssenceThiefDelivery>(), 0, 0, Player.whoAmI, 2, 1);
            }
        }
    }
}