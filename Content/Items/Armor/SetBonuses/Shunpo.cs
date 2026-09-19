using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;

namespace tsorcRevamp.Content.Items.Armor.SetBonuses;

public class Shunpo : ModPlayer
{
    public bool HasShunpo;
    public const int ShunpoCooldownPerHit = -60;
    public override void ResetEffects()
    {
        HasShunpo = false;
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.Shunpo.JustReleased && HasShunpo)
        {
            DoShunpo();
        }
    }

    public override void ArmorSetBonusActivated()
    {
        if (HasShunpo)
        {
            DoShunpo();
        }
    }

    public void DoShunpo()
        {
            List<NPC> validTargets = new List<NPC>();
            NPC chosenTarget = Main.npc.Last();
            foreach (var other in Main.ActiveNPCs)
            {
                Vector2 MouseHitboxSize = new Vector2(100, 100);
                
                bool lineOfSight = Collision.CanHitLine(Player.position, Player.width, Player.height, Main.MouseWorld, Player.width, Player.height);

                if (lineOfSight && !tsorcRevamp.UntargetableNPCs.Contains(other.type) && !other.friendly 
                    && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)) 
                    && !Player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
                {
                    validTargets.Add(other);
                }
            }

            foreach (var target in validTargets)
            {
                if (target.Center.Distance(Main.MouseWorld) < chosenTarget.Center.Distance(Main.MouseWorld))
                {
                    chosenTarget = target; //so it actually ports you to the npc nearest to your cursor, not any random npc that is in the cursors range
                }
            }

            if (chosenTarget != Main.npc.Last())
            {
                Player.Center += Player.DirectionTo(chosenTarget.Center) * Main.MouseWorld.Distance(Player.Center);
                Player.RefreshMovementAbilities();
                Player.AddBuff(ModContent.BuffType<ShunpoBlink>(), (int)(ShunpoBlink.ShunpoBlinkImmunityTime * 60));
                Player.immune = true;
                Player.SetImmuneTimeForAllTypes((int)(ShunpoBlink.ShunpoBlinkImmunityTime * 60));
                Player.AddBuff(ModContent.BuffType<ShunpoBlinkCooldown>(), ShunpoBlink.Cooldown );
                if (Main.rand.NextBool(2))
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo1") with { Volume = 1f });
                }
                else
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo2") with { Volume = 1f });
                }
                //ShunpoTimer = 3;
            }
        }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        ShunpoSummonTitaniumShard(target);
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (proj.type != ProjectileID.TitaniumStormShard)
        {
            ShunpoSummonTitaniumShard(target);
        }
    }

    public void ShunpoSummonTitaniumShard(NPC target)
    {
        if (HasShunpo && Player.titaniumStormCooldown <= 0 && target.type != NPCID.TargetDummy)
        {
            int TitaniumShardTotalDmg = (int)Player.GetDamage(DamageClass.Generic).ApplyTo(50); //50 is base dmg of titanium shards
            Player.titaniumStormCooldown = 10;
            Player.AddBuff(BuffID.TitaniumStorm, 10 * 60);
            if (Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard] < 15)
            {
                Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard]++;
                if (Main.myPlayer == Player.whoAmI)
                {
                    Projectile.NewProjectile(Player.GetSource_OnHit(target), Player.Center, Vector2.Zero, ProjectileID.TitaniumStormShard, TitaniumShardTotalDmg, 15f, Player.whoAmI);
                }
            }
            else if (Player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
            {
                UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<ShunpoBlinkCooldown>(), ShunpoCooldownPerHit);
            }
        }
    }

    /*public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) //No longer necessary since Shunpo adjusts player position now instead of velocity
    {
        if (ShunpoTimer > 0 && (proj.type == ProjectileID.JoustingLance || proj.type == ProjectileID.HallowJoustingLance || proj.type == ProjectileID.ShadowJoustingLance))
        {
            modifiers.FinalDamage *= 0.15f;
        }
    }*/

    public override void PostUpdateEquips()
    {
        if (HasShunpo && !Player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (other.active && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, Player.GetModPlayer<tsorcRevampPlayer>().MouseHitboxSize)))
                {
                    Lighting.AddLight(other.Center, Color.Red.ToVector3() * 0.35f);
                    UsefulFunctions.DustRing(other.Center, other.width / 2, DustID.Titanium, 5, 1);
                    UsefulFunctions.DustRing(other.Center, other.width / 4, DustID.Adamantite, 5, 2);
                }
            }
        }
    }
}