using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;


    public class RuneterraDartsPlayer : ModPlayer
    {
        public bool ScoutsHeldItem;
        public bool AlienScoutsHeldItem;
        public bool OmegaScoutsHeldItem;
        public bool ScoutsBoostPassive;
        public bool ScoutsBoostActive;

        public int ShroomCount = RuneterraDarts.MaxShrooms;
        public int ShroomTimer = 0;
        public override void ResetEffects()
        {
            ScoutsHeldItem  = false;
            AlienScoutsHeldItem  = false;
            OmegaScoutsHeldItem = false;
            ScoutsBoostPassive = false;
            ScoutsBoostActive = false;
            if (Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>() && ShroomCount < 5)
            {
                ShroomTimer++;
                if (ShroomTimer >= OmegaSquadRifle.ShroomCooldown * 60)
                {
                    ShroomTimer = 0;
                    ShroomCount++;
                    if (ShroomCount == RuneterraDarts.MaxShrooms)
                    {
                        SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound)) + "_ShroomReady") with { Volume = 1f });
                    }
                    else
                    { 
                        Player.AddBuff(ModContent.BuffType<NuclearMushroomCooldown>(),
                            (OmegaSquadRifle.ShroomCooldown * 60) - ShroomTimer);
                    }
                }
            }
        }
        public void CustomPostUpdateMiscEffects()
        {
            float velocityMult1 = 0.93f;
            float velocityMult2 = 0;
            if (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() || Player.HeldItem.type == ModContent.ItemType<AlienGun>())
            {
                if (!Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()) && !Player.HasBuff(ModContent.BuffType<ScoutsBoost2>()))
                {
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 2);
                }
                if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoost>())) //Scouts Boost 2 blocks Scouts Boost 1 and its cooldown so this won't occur then
                {
                    Player.velocity.X *= velocityMult1;
                }
                else if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldown>()))
                {
                    Player.velocity.X *= velocityMult2;
                }
            }
            if (Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
            {
                if (!Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>()) && !Player.HasBuff(ModContent.BuffType<ScoutsBoost2Omega>()))
                {
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost>(), 2); //ScoutsBoost buff itself does not play any sounds in it's code so I didn't need to make an Omega version
                }
                if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoost>())) //Scouts Boost 2 blocks Scouts Boost 1 and its cooldown so this won't occur then
                {
                    Player.velocity.X *= velocityMult1;
                }
                else if (Player.itemAnimation > 1 && Player.HasBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>()))
                {
                    Player.velocity.X *= velocityMult2;
                }
            }
            if (ScoutsBoostPassive)
            {
                Player.moveSpeed *= 1f + ToxicShot.ScoutsBoostMoveSpeedMult / 100f;
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + ToxicShot.ScoutsBoostStaminaRegenMult / 100f; //this runs right before stamina regen is calculated, meaning it will multiply all of your additive bonuses properly
            }

            if (ScoutsBoostActive)
            {
                Player.moveSpeed *= 1f + ToxicShot.ScoutsBoostMoveSpeedMult * 2f / 100f;
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + ToxicShot.ScoutsBoostStaminaRegenMult * 2f / 100f;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() | Player.HeldItem.type == ModContent.ItemType<AlienGun>() && !Main.player[Main.myPlayer].HasBuff(ModContent.BuffType<ScoutsBoost2>()))
            {
                Player.AddBuff(ModContent.BuffType<ScoutsBoostCooldown>(), ToxicShot.ScoutsBoostOnHitCooldown * 60);
            }
            if (Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>() && !Main.player[Main.myPlayer].HasBuff(ModContent.BuffType<ScoutsBoost2Omega>()))
            {
                Player.AddBuff(ModContent.BuffType<ScoutsBoostCooldownOmega>(), ToxicShot.ScoutsBoostOnHitCooldown * 60);
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (tsorcRevamp.specialAbility.JustReleased && !Player.dead)
            {
                if (!Player.HasBuff(ModContent.BuffType<ScoutsBoost2Cooldown>()) && (Player.HeldItem.type == ModContent.ItemType<ToxicShot>() | Player.HeldItem.type == ModContent.ItemType<AlienGun>()))
                {
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost2>(), ToxicShot.ScoutsBoost2Duration * 60);
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost2Cooldown>(), ToxicShot.ScoutsBoost2Cooldown * 60);
                }
                if (!(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && !Player.HasBuff(ModContent.BuffType<ScoutsBoost2CooldownOmega>()) && Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
                {
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost2Omega>(), ToxicShot.ScoutsBoost2Duration * 60);
                    Player.AddBuff(ModContent.BuffType<ScoutsBoost2CooldownOmega>(), ToxicShot.ScoutsBoost2Cooldown * 60);
                }
                if ((Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && ShroomCount > 0 && Player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
                {
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Projectile Shroom = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<NuclearMushroom>(), Player.GetWeaponDamage(Player.HeldItem), Player.GetWeaponKnockback(Player.HeldItem), Main.myPlayer);
                    }

                    ShroomCount--;
                    if (!Player.HasBuff(ModContent.BuffType<NuclearMushroomCooldown>()))
                    {
                        Player.AddBuff(ModContent.BuffType<NuclearMushroomCooldown>(),
                            (OmegaSquadRifle.ShroomCooldown * 60) - ShroomTimer);
                    }
                }

            }
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            if (ShroomCount < 5)
            {
                ShroomCount = 5;
                ShroomTimer = 0;
            }
        }
    }