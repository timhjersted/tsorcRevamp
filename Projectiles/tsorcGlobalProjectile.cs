using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Armors.Ranged;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.Enemy.Golem;
using tsorcRevamp.Projectiles.Melee;
using tsorcRevamp.Projectiles.Melee.Spears;
using tsorcRevamp.Projectiles.Pets;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Utilities;
using MiakodaCrescent = tsorcRevamp.Projectiles.Pets.MiakodaCrescent;
using MiakodaNew = tsorcRevamp.Projectiles.Pets.MiakodaNew;
using tsorcRevamp.Items.Weapons.Summon;

namespace tsorcRevamp.Projectiles
{
    class tsorcGlobalProjectile : GlobalProjectile
    {
        /*
        public override bool InstancePerEntity => true;
        public float UniqueIdentifier = -1;

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(UniqueIdentifier);
            base.SendExtraAI(projectile, bitWriter, binaryWriter);
        }
        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            UniqueIdentifier = binaryReader.ReadSingle();
            base.ReceiveExtraAI(projectile, bitReader, binaryReader);
        }
        public override void SetDefaults(Projectile projectile)
        {
            if(Main.netMode != NetmodeID.MultiplayerClient)
            {
                UniqueIdentifier = UsefulFunctions.CreateUniqueIdentifier();
            }
            base.SetDefaults(projectile);
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.ModProjectile is DynamicTrail) {
                if (source is EntitySource_Parent { Entity: Projectile } projParent)
                {
                    projectile.ai[1] = ((Projectile)projParent.Entity).GetGlobalProjectile<tsorcGlobalProjectile>().UniqueIdentifier;
                    projectile.netUpdate = true;
                }
                else if (source is EntitySource_Parent { Entity: NPC } NPCparent)
                {
                    projectile.ai[1] = NPCparent.Entity.whoAmI;
                    projectile.netUpdate = true;
                }
            }

            base.OnSpawn(projectile, source);
        }*/
        public override bool InstancePerEntity => true;
        public static float WhipVolume = 0.4f;
        public static float WhipPitch = 0.3f;
        public bool AppliedLethalTempo = false;
        public bool AppliedConqueror = false;
        public bool IgnoresAccuracyOrSpecialCase = false;
        public bool HitSomething = false;
        public bool ModdedWhip = false;
        public bool ChargedWhip = false;
        public bool ModdedFlail = false;
        public bool KrakenEmpowered = false;
        public override void SetDefaults(Projectile entity)
        {
            if (entity.IsMinionOrSentryRelated || ProjectileID.Sets.LightPet[entity.type] || Main.projPet[entity.type])
            {
                ProjectileID.Sets.ForcePlateDetection[entity.type] = false;
            }
            if (tsorcRevamp.WhipRanges.ContainsKey(entity.type))
            {
                entity.WhipSettings.RangeMultiplier = tsorcRevamp.WhipRanges[entity.type];
            }
        }
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            /*projectilesource experiments
             * if (projectile.type == ModContent.ProjectileType<Projectiles.Spears.FetidExhaust>())
            {
                projectileSource_ItemUse_WithAmmo itemSource = source as projectileSource_ItemUse_WithAmmo;

                if (itemSource != null && itemSource.Item.type == ModContent.ItemType<Items.Weapons.Melee.Spears.FetidExhaust>())
                {
                    Main.NewText("a");
                }
            }*/
            Player player = Main.player[projectile.owner];
            if (projectile.friendly)
            {
                if (projectile.type == ProjectileID.CrystalDart)
                {
                    projectile.damage = 1 + player.GetWeaponDamage(player.HeldItem);
                }
                if (player.GetModPlayer<tsorcRevampPlayer>().Goredrinker && !player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && projectile.DamageType == DamageClass.SummonMeleeSpeed && player.GetModPlayer<tsorcRevampPlayer>().GoredrinkerReady
                    && ProjectileID.Sets.IsAWhip[projectile.type] && !ModdedWhip) //Modded whips have this in their code itself because some of them can be charged
                {
                    player.GetModPlayer<tsorcRevampPlayer>().GoredrinkerSwung = true;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GoredrinkerSwing") with { Volume = .3f }, player.Center);
                }
            }
            if (projectile.type == ProjectileID.Fireball && NPC.AnyNPCs(NPCID.Golem))
            {
                int extraUpdates = 2;
                projectile.extraUpdates = extraUpdates;
                projectile.timeLeft = 600 * (1 + extraUpdates);
                projectile.scale = 2f;
            }
            if (projectile.type == ProjectileID.DD2ExplosiveTrapT1Explosion && projectile.ai[0] == 1)
            {
                projectile.ArmorPenetration = 10;
                projectile.ai[0] = 0;
            }
            if (projectile.type == ProjectileID.Skull && projectile.ai[0] == 1)
            {
                projectile.velocity = (projectile.position - player.Center) / 5;
                projectile.DamageType = DamageClass.MagicSummonHybrid;
                projectile.ai[0] = 0;
            }
            if (projectile.type == ProjectileID.Muramasa && projectile.ai[0] == 1)
            {
                projectile.CritChance = (int)player.GetTotalCritChance(DamageClass.Melee) + player.HeldItem.crit;
                projectile.ai[0] = 0;
            }
            if (projectile.type == ProjectileID.Flamelash && projectile.ai[0] == 1)
            {
                projectile.DamageType = DamageClass.SummonMeleeSpeed;
                player.GetModPlayer<tsorcRevampPlayer>().DragoonLashFireBreathTimer = 0;
                projectile.ai[0] = 0;
            }
            if (projectile.type == ProjectileID.ScytheWhipProj && projectile.ai[0] == 1)
            {
                projectile.localNPCImmunity[(int)projectile.ai[1]] = -1;
                projectile.ai[0] = 0;
                projectile.ai[1] = 0;
            }
            if (projectile.type == ProjectileID.HoundiusShootiusFireball)
            {
                projectile.penetrate = 2;
                projectile.usesLocalNPCImmunity = true;
                projectile.localNPCHitCooldown = -1;
            }
            if (projectile.type == ProjectileID.CoolWhipProj && projectile.ai[0] == 1)
            {
                projectile.ai[0] = 0;
                projectile.ArmorPenetration = 30;
            }
        }
        public override bool PreAI(Projectile projectile)
        {
            if (projectile.owner < Main.maxPlayers && Main.player[projectile.owner].active)
            {
                Player player = Main.player[projectile.owner];
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

                if (projectile.type == ProjectileID.Terrarian && player.statMana >= (int)(player.manaCost * 2f))
                {
                    player.statMana -= (int)(player.manaCost * 2f);
                }
                else
                {
                    if (projectile.type == ProjectileID.Terrarian)
                    {
                        projectile.Kill();
                    }
                }

                if (projectile.type == ProjectileID.MoonlordTurretLaser)
                {
                    projectile.usesLocalNPCImmunity = false;
                    projectile.localNPCHitCooldown = -2;
                    projectile.usesIDStaticNPCImmunity = true;
                    projectile.idStaticNPCHitCooldown = 10;
                }

                if (modPlayer.WaspPower & projectile.type == ProjectileID.HornetStinger)
                {
                    projectile.penetrate = 6;
                    projectile.usesLocalNPCImmunity = true;
                    projectile.localNPCHitCooldown = 20;
                    projectile.extraUpdates = 5;
                }

                else if (!modPlayer.WaspPower & projectile.type == ProjectileID.HornetStinger)
                {
                    projectile.penetrate = 1;
                    projectile.usesLocalNPCImmunity = false;
                }

                if (projectile.type == ProjectileID.BloodArrow)
                {
                    projectile.tileCollide = false;
                    projectile.ai[2]++;
                    if (projectile.ai[2] > 180)
                    {
                        projectile.Kill();
                    }
                }

                if (projectile.type == ProjectileID.FrostBlastFriendly)
                {
                    projectile.penetrate = 6;
                    projectile.usesIDStaticNPCImmunity = false;
                    projectile.localNPCHitCooldown = 100;
                    projectile.usesLocalNPCImmunity = true;
                    projectile.extraUpdates = 3;
                }

                if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaCrescent>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>()))
                {
                    if (Main.rand.NextBool(2))
                    {
                        Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 164, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        dust.noGravity = false;
                    }
                }

                if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaNew>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>()))
                {
                    if (Main.rand.NextBool(2))
                    {
                        Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 57, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 130, default(Color), 1f);
                        dust.noGravity = true;
                    }
                }
                if (projectile.owner == Main.myPlayer && !projectile.hostile && projectile.DamageType == DamageClass.Melee)
                {
                    if (modPlayer.MagicWeapon)
                    {
                        Lighting.AddLight(projectile.position, 0.3f, 0.3f, 0.45f);
                        for (int i = 0; i < 4; i++)
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            dust.noGravity = true;
                        }
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 15, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            dust.noGravity = true;
                        }
                    }

                    if (modPlayer.GreatMagicWeapon)
                    {
                        Lighting.AddLight(projectile.position, 0.3f, 0.3f, 0.55f);
                        for (int i = 0; i < 4; i++)
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 172, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            dust.noGravity = true;
                        }
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            dust.noGravity = true;
                        }
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 172, player.velocity.X * .2f, player.velocity.Y * 0.2f, 30, default(Color), 1.3f);
                            dust.noGravity = true;
                        }
                    }

                    if (modPlayer.CrystalMagicWeapon)
                    {
                        Lighting.AddLight(projectile.position, 0.3f, 0.3f, 0.55f);
                        for (int i = 0; i < 2; i++)
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 221, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            dust.noGravity = true;
                        }
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            dust.noGravity = true;
                        }
                        {
                            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 172, player.velocity.X * .2f, player.velocity.Y * 0.2f, 30, default(Color), 1.3f);
                            dust.noGravity = true;
                        }
                    }
                }

                if (projectile.owner == Main.myPlayer && (projectile.aiStyle == ProjAIStyleID.Flail || projectile.aiStyle == ProjAIStyleID.Yoyo || projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ModdedFlail
                    ) && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 1)
                {
                    //projectile.Kill();

                    if (projectile.aiStyle == ProjAIStyleID.Yoyo)
                    {
                        projectile.ai[0] = -1; //return yoyo smoothly, dont just kill it. This took me ages to find :( (doesn't work)
                    }
                    else if (projectile.aiStyle == ProjAIStyleID.Flail || projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ModdedFlail)
                    {
                        projectile.ai[1] = 1; //return flail smoothly, dont just kill it (doesn't work)
                    }
                    else projectile.Kill();
                }

                #region Attempt at allowing you to stack Lethal Tempo by grazing enemy projectiles with a melee weapon, rarely works
                /*for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];

                    if ((projectile.DamageType == DamageClass.Melee || projectile.DamageType == DamageClass.MeleeNoSpeed) && modPlayer.BearerOfTheCurse && projectile.Hitbox.Intersects(other.Hitbox) &&
                        i != projectile.whoAmI && other.active && !other.friendly && other.hostile && other.damage > 0 && UsefulFunctions.IsProjectileSafeToFuckWith(i) && other.type != ModContent.ProjectileType<Nothing>() && other.type != ModContent.ProjectileType<Slash>() && !AppliedLethalTempo)
                    {
                        if (modPlayer.BotCLethalTempoStacks < modPlayer.BotCLethalTempoMaxStacks - 1)
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.4f }, player.Center);
                        }
                        else if (modPlayer.BotCLethalTempoStacks == modPlayer.BotCLethalTempoMaxStacks - 1)
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 2f }, player.Center);
                        }
                        player.AddBuff(ModContent.BuffType<LethalTempo>(), player.GetModPlayer<tsorcRevampPlayer>().BotCLethalTempoDuration * 60);
                        AppliedLethalTempo = true;
                        other.GetGlobalProjectile<tsorcGlobalProjectile>().AppliedLethalTempo = true;
                    }
                }*/
                #endregion

            }
            if (projectile.type == ProjectileID.PhantasmalDeathray) //die
            {
                projectile.damage = 1000;
            }
            if (projectile.type == ProjectileID.PhantasmalBolt) //the double shot
            {
                projectile.damage = 75;
            }
            if (projectile.type == ProjectileID.PhantasmalSphere) //the big circly eyes that go after you
            {
                projectile.damage = 130;
            }
            if (projectile.type == ProjectileID.PhantasmalEye) //the things that home in towards you and explode
            {
                projectile.damage = 90;
            }

            //Destroyer shoots true lasers instead of normal projectile lasers
            //Probe lasers are replaced with true lasers. This is actually an enormous nerf because they were not telegraphed and were hard to see before.
            if (NPC.AnyNPCs(NPCID.TheDestroyer))
            {
                if (projectile.type == ProjectileID.DeathLaser)
                {
                    projectile.Kill();
                }
                if (projectile.type == ProjectileID.PinkLaser)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, projectile.velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 40, 0, Main.myPlayer, -2, -1);
                    }
                    projectile.Kill();
                }
            }


            if (projectile.type == ProjectileID.DD2SquireSonicBoom && projectile.ai[2] != 0)
            {
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 156, 0, 0, 0, default(Color), 1f);
                    dust.noGravity = true;
                }
            }


            return true;


        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && projectile.type != (int)ModContent.ProjectileType<MiakodaCrescent>())
            {
                target.AddBuff(ModContent.BuffType<Buffs.CrescentMoonlight>(), 3 * 60); // Adds the ExampleJavelin debuff for a very small DoT
            }

            if (projectile.type == ProjectileID.BabySlime)
            {
                target.AddBuff(BuffID.Slow, 60);
            }

            if (projectile.type == ProjectileID.FlinxMinion)
            {
                target.AddBuff(BuffID.Frostburn, 60);
            }
            
            if(entity.type == ProjectileID.BabyBird)
            {
                entity.minionSlots = 2f;
            }


            if (projectile.type == ProjectileID.VampireFrog)
            {
                player.statLife += modifiers.GetDamage(modifiers.SourceDamage.ApplyTo(Item.damage) / 1, true);
                player.HealEffect(modifiers.GetDamage(modifiers.SourceDamage.ApplyTo(Item.damage) / 1, true));


            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && projectile.type != (int)ModContent.ProjectileType<MiakodaNew>())
            {
                target.AddBuff(BuffID.Midas, 5 * 60);
            }

            if (projectile.owner == Main.myPlayer && (modPlayer.MagicWeapon || modPlayer.GreatMagicWeapon) && projectile.DamageType == DamageClass.Melee)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit44 with { Volume = 0.3f }, target.position);
            }
            if (projectile.owner == Main.myPlayer && modPlayer.CrystalMagicWeapon && projectile.DamageType == DamageClass.Melee)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.3f }, target.position);
            }

            if ((projectile.type >= ProjectileID.MonkStaffT3 && projectile.type <= ProjectileID.DD2BetsyArrow || (projectile.type == ProjectileID.DD2SquireSonicBoom && projectile.ai[2] == 0)) && Condition.DownedOldOnesArmyT3.IsMet())
            {
                target.AddBuff(BuffID.BetsysCurse, EtherianWyvernStaff.DebuffDuration * 60);
            }

            #region Runeterra Poison Darts
            if (projectile.type == ProjectileID.PoisonDartBlowgun && player.HeldItem.type == ModContent.ItemType<ToxicShot>())
            {
                if (hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/ShotCrit") with { Volume = 0.5f }, target.Center);
                }
                else
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/ShotHit") with { Volume = 0.5f }, target.Center);
                }
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[projectile.owner];
                target.AddBuff(ModContent.BuffType<VenomDebuff>(), 2 * 60);
            }
            if (projectile.type == ProjectileID.PoisonDartBlowgun && player.HeldItem.type == ModContent.ItemType<AlienGun>())
            {
                if (hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/ShotCrit") with { Volume = 0.5f }, target.Center);
                }
                else
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/ShotHit") with { Volume = 0.5f }, target.Center);
                }
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[projectile.owner];
                target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), 2 * 60);
            }
            if (projectile.type == ProjectileID.PoisonDartBlowgun && player.HeldItem.type == ModContent.ItemType<OmegaSquadRifle>())
            {
                if (hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShotCrit") with { Volume = 0.5f }, target.Center);
                }
                else
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShotHit") with { Volume = 0.5f }, target.Center);
                }
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[projectile.owner];
                target.AddBuff(ModContent.BuffType<IrradiatedDebuff>(), 2 * 60);
            }
            #endregion

            #region Lethal Tempo
            if ((projectile.DamageType == DamageClass.Melee || projectile.DamageType == DamageClass.MeleeNoSpeed) && modPlayer.BearerOfTheCurse && !AppliedLethalTempo)
            {
                if (modPlayer.BotCLethalTempoStacks < modPlayer.BotCLethalTempoMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.002f }, player.Center);
                }
                else if (modPlayer.BotCLethalTempoStacks == modPlayer.BotCLethalTempoMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.003f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<LethalTempo>(), player.GetModPlayer<tsorcRevampPlayer>().BotCLethalTempoDuration * 60);
                if (projectile.type != ModContent.ProjectileType<FetidExhaustProjectile>())
                {
                    AppliedLethalTempo = true;
                }
            }
            #endregion

            #region Conqueror
            if (projectile.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[projectile.type] && modPlayer.BearerOfTheCurse && !AppliedConqueror)
            {
                if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks - 1 && !hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks - 2 && hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.008f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks == modPlayer.BotCConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks == modPlayer.BotCConquerorMaxStacks - 2 && hit.Crit)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().BotCConquerorDuration * 60);
                if (hit.Crit)
                {
                    player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().BotCConquerorDuration * 60);
                }
                AppliedConqueror = true;
            }
            else if (projectile.DamageType != DamageClass.Summon && projectile.DamageType != DamageClass.SummonMeleeSpeed && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.ZoneOldOneArmy)
            {
                if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (modPlayer.BotCConquerorStacks == modPlayer.BotCConquerorMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<Conqueror>(), player.GetModPlayer<tsorcRevampPlayer>().BotCConquerorDuration * 60);
                AppliedConqueror = true;
            }
            #endregion

            #region Accuracy            
            if (!IsAccuracySpecialCase(projectile))
            {
                HitSomething = true;
            }
            #endregion
        }

        /// <summary>
        /// Simply returns true if the projectile is a special case that isn't supposed to count for or against accuracy (like explosions, projectiles spawned by projectiles, homing ones, purely visual ones, etc
        /// Projectile *types* that are always special cases should just go in PopulateAccuracySpecialCases(), this is mainly for more complex stuff (like checking the projectile's ai[] fields)
        /// </summary>
        /// <param name="projectile"></param>
        /// <returns></returns>
        public static bool IsAccuracySpecialCase(Projectile projectile)
        {
            return AccuracySpecialCaseList.Contains(projectile.type) || (projectile.type == ProjectileID.Bone && projectile.ai[2] == 1);
        }


        //This loads the list automatically the first time someone tries to access it
        //It works by checking if the list is null, loading it if it is, then returning it        
        public static List<int> AccuracySpecialCaseList
        {
            get
            {
                if (AccuracySpecialCases == null)
                {
                    PopulateAccuracySpecialCases();
                }

                return AccuracySpecialCases;
            }
        }

        /// <summary>
        /// This is where the list of all accuracy special case projectiles go
        /// Works like all the other PopulateX() functions we have
        /// </summary>
        private static void PopulateAccuracySpecialCases()
        {
            AccuracySpecialCases = new List<int>()
            {
               ModContent.ProjectileType<ElfinArrow>(), ModContent.ProjectileType<ToxicCatExplosion>(), ModContent.ProjectileType<VirulentCatExplosion>(), ModContent.ProjectileType<BiohazardExplosion>(),
               ModContent.ProjectileType<KrakenTsunamiShark>(), ProjectileID.CrystalShard, ModContent.ProjectileType<ShulletBellDark>(),  ModContent.ProjectileType<ShulletBellLight>(),
               ProjectileID.ChlorophyteBullet, ProjectileID.ChlorophyteArrow, ProjectileID.HallowStar,
               ProjectileID.DD2PhoenixBow
            };
        }

        public static List<int> AccuracySpecialCases;

        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            if (projectile.type == ProjectileID.EyeLaser && projectile.ai[0] == 1)
            {
                target.AddBuff(BuffID.Slow, 3 * 60);
            }

            if (projectile.type == ProjectileID.DemonSickle)
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), 15);
                target.AddBuff(BuffID.Slow, 3 * 60);
                target.AddBuff(BuffID.Darkness, 3 * 60);
            }
            if (projectile.type == ProjectileID.Bubble)
            {
                SoundEngine.PlaySound(SoundID.Drown, target.Center);
                target.AddBuff(BuffID.Chilled, 8 * 60);
                target.AddBuff(ModContent.BuffType<Gilled>(), 16 * 60);
            }
        }
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[projectile.owner];
            if (!Main.hardMode && (projectile.type == ProjectileID.StarCloakStar || projectile.type == ProjectileID.StarVeilStar || projectile.type == ProjectileID.BeeCloakStar || projectile.type == ProjectileID.ManaCloakStar))
            {
                modifiers.FinalDamage *= 0.1f;
            }
            if (tsorcRevampWorld.NewSlain != null)
            {
                if (projectile.type == ProjectileID.EmpressBlade & !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())))
                {
                    modifiers.FinalDamage *= 0.76f;
                }
                else if (projectile.type == ProjectileID.EmpressBlade & !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
                {
                    modifiers.FinalDamage *= 0.89f;
                }
            }
            if (KrakenEmpowered)
            {
                modifiers.SourceDamage += KrakenCarcass.TsunamiDmgBoost / 100f;
            }
        }

        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
        }

        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            if (projectile.type == ProjectileID.SandBallFalling && projectile.velocity.X != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
            /*if (projectile.type == ProjectileID.TowerDamageBolt)
            {
                if (NPC.AnyNPCs(NPCID.LunarTowerVortex))
                {
                    Main.NewText("Tower existtttttttttttttttttttt.");
                    
                }
                else
                {
                    Main.NewText("Tower gone gone gone gone gone.");
                }
                if (Main.projectile[ProjectileID.TowerDamageBolt].active)
                {
                    Main.NewText("Bolt exist.");
                }
                if ((int)projectile.ai[0] == -1)
                {
                    Main.NewText("Because target none");
                }
                if (!Main.npc[(int)projectile.ai[0]].active)
                {
                    Main.NewText("Because tower inactive");
                }
                Main.NewText("TowerDamageBolt killed");
            }
            return true;*/
        }


        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (projectile.type == ProjectileID.Fireball && NPC.AnyNPCs(NPCID.Golem))
            {
                int Difficulty = 1 + (Main.expertMode ? 1 : 0) + (Main.masterMode ? 1 : 0);
                Vector2 Vel = Main.rand.NextVector2CircularEdge(20, 20);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, projectile.velocity + Vel, ModContent.ProjectileType<SmallGolemFireball>(), projectile.damage / 2, projectile.knockBack / 2, Main.myPlayer);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, projectile.velocity - Vel, ModContent.ProjectileType<SmallGolemFireball>(), projectile.damage / 2, projectile.knockBack / 2, Main.myPlayer);
                }
                if (NPC.CountNPCS(NPCID.SolarCorite) < 3)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC Corite = NPC.NewNPCDirect(NPC.GetSource_NaturalSpawn(), projectile.Center, NPCID.SolarCorite);

                        Corite.lifeMax = 125 * Difficulty; //main problem is too many of them spawned at one time
                        Corite.life = Corite.lifeMax;
                        Corite.knockBackResist = 0.3f;

                        Corite.damage = 60 * Difficulty;

                        Corite.value = 100 * Difficulty;
                        Corite.netUpdate = true;
                    }
                }
            }
            if (projectile.friendly && !projectile.hostile)
            {
                Player owner = Main.player[projectile.owner];
                var modPlayer = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
                if (modPlayer.Goredrinker && !owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && projectile.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[projectile.type] && modPlayer.GoredrinkerSwung)
                {
                    owner.AddBuff(ModContent.BuffType<GoredrinkerCooldown>(), Items.Accessories.Summon.Goredrinker.Cooldown * 60);
                    modPlayer.GoredrinkerReady = false;
                    modPlayer.GoredrinkerSwung = false;
                }

                if (!IsAccuracySpecialCase(projectile) && projectile.DamageType == DamageClass.Ranged && modPlayer.BearerOfTheCurse && projectile.damage != 0)
                {
                    if (HitSomething)
                    {
                        modPlayer.BotCCurrentAccuracyPercent += modPlayer.BotCAccuracyGain;
                        CombatText.NewText(owner.Hitbox, Color.BurlyWood, LangUtils.GetTextValue("UI.BotCHit", (int)(MathF.Min(modPlayer.BotCCurrentAccuracyPercent, 1f) * 100f)));
                    }
                    else if (!HitSomething)
                    {
                        modPlayer.BotCCurrentAccuracyPercent -= modPlayer.BotCAccuracyLoss;
                        CombatText.NewText(owner.Hitbox, Color.BurlyWood, LangUtils.GetTextValue("UI.BotCMiss", (int)(MathF.Max(modPlayer.BotCCurrentAccuracyPercent, 0) * 100f)));
                    }
                    if (modPlayer.BotCCurrentAccuracyPercent > modPlayer.BotcAccuracyPercentMax)
                    {
                        modPlayer.BotCCurrentAccuracyPercent = modPlayer.BotcAccuracyPercentMax;
                    }
                    if (modPlayer.BotCCurrentAccuracyPercent < 0)
                    {
                        modPlayer.BotCCurrentAccuracyPercent = 0;
                    }
                }
            }

            if (projectile.type == ProjectileID.DD2SquireSonicBoom && projectile.ai[2] != 0)
            {
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustDirect(new Vector2(projectile.position.X - 50, projectile.position.Y - 50), 100, 100, 156, 0, 0, 0, default(Color), 1f);
                    dust.noGravity = true;
                    dust.velocity.Y *= Main.rand.NextFloat(-3f, 3f);
                    dust.velocity.X *= Main.rand.NextFloat(-3f, 3f);
                }
            }

            base.OnKill(projectile, timeLeft);
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (projectile.type == ProjectileID.DD2SquireSonicBoom && projectile.ai[2] != 0)
            {
                Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[projectile.type];
                Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, null, Color.LightCyan, projectile.rotation, texture.Size() / 2f, projectile.scale, SpriteEffects.None, 0);
                return false;
            }

            return base.PreDraw(projectile, ref lightColor);
        }

    }
}
