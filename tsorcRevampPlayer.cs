using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Items;

namespace tsorcRevamp {
    public class tsorcRevampPlayer : ModPlayer {

        public int warpX;
        public int warpY;
        public int warpWorld;
        public bool warpSet;

        public int townWarpX;
        public int townWarpY;
        public int townWarpWorld;
        public bool townWarpSet;

        public bool SilverSerpentRing = false;
        public bool DragonStone = false;
        public int SoulReaper = 0;

        public bool DuskCrownRing = false;
        public bool UndeadTalisman = false;

        public bool DragoonBoots = false;
        public bool DragoonBootsEnable = false;

        public bool GemBox = false;
        public bool ConditionOverload = true;

        public int CurseLevel = 1;
        public bool DarkInferno = false;
        public bool CrimsonDrain = false;

        public bool Shockwave = false;
        public bool Falling;
        public int StopFalling;
        public float FallDist;
        public float fallStartY;

        public bool MeleeArmorVamp10 = false;
        public bool NUVamp = false;

        public bool OldWeapon = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;

        public int souldroplooptimer = 0;
        public int souldroptimer = 0;


        public override TagCompound Save() {
            return new TagCompound {
            {"warpX", warpX},
            {"warpY", warpY},
            {"warpWorld", warpWorld},
            {"warpSet", warpSet},
            {"townWarpX", townWarpX},
            {"townWarpY", townWarpY},
            {"townWarpWorld", townWarpWorld},
            {"townWarpSet", townWarpSet},
            };

        }

        public override void Load(TagCompound tag) {
            warpX = tag.GetInt("warpX");
            warpY = tag.GetInt("warpY");
            warpWorld = tag.GetInt("warpWorld");
            warpSet = tag.GetBool("warpSet");
            townWarpX = tag.GetInt("townWarpX");
            townWarpY = tag.GetInt("townWarpY");
            townWarpWorld = tag.GetInt("townWarpWorld");
            townWarpSet = tag.GetBool("townWarpSet");
        }

        public override void ResetEffects() {
            SilverSerpentRing = false;
            DragonStone = false;
            SoulReaper = 0;
            DragoonBoots = false;
            player.eocDash = 0;
            player.armorEffectDrawShadowEOCShield = false;
            UndeadTalisman = false;
            DuskCrownRing = false;
            DragoonBoots = false;
            GemBox = false;
            OldWeapon = false;
            Miakoda = false;
            RTQ2 = false;
            DarkInferno = false;
            BoneRevenge = false;
            CrimsonDrain = false;
            Shockwave = false;
            souldroplooptimer = 0;
            souldroptimer = 0;
        }

        public override void PostUpdateEquips() {
            if (Shockwave) {
                if (player.controlDown && player.velocity.Y != 0f) {
                    player.gravity += 5f;
                    player.maxFallSpeed *= 1.25f;
                    if (!Falling) {
                        fallStartY = player.position.Y;
                    }
                    if (player.velocity.Y > 12f) {
                        Falling = true;
                        StopFalling = 0;
                        player.noKnockback = true;
                    }
                }
                if (player.velocity.Y == 0f && Falling && player.controlDown) {
                    for (int i = 0; i < 30; i++) {
                        int dustIndex2 = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 31, 0f, 0f, 100);
                        Main.dust[dustIndex2].scale = 0.1f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].fadeIn = 1.5f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].noGravity = true;
                    }
                    for (int i = -8; i < 9; i++) {
                        Vector2 shotDirection = new Vector2(0f, -16f);
                        FallDist = (int)((player.position.Y - fallStartY) / 16);
                        
                        int shockwaveShot = Projectile.NewProjectile(player.Center, new Vector2(0f, -7f), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * 2.75f), 12, player.whoAmI);
                        Main.projectile[shockwaveShot].velocity = shotDirection.RotatedBy(MathHelper.ToRadians(0 - (11.25f * i))); //lerp wasnt working, so do manual interpolation
                    }

                    Main.PlaySound(SoundID.Item, (int)player.Center.X, (int)player.Center.Y, 14);
                    Falling = false;
                }
                if (player.velocity.Y <= 2f) {
                    StopFalling++;
                }
                else {
                    StopFalling = 0;
                }
                if (StopFalling > 1) {
                    Falling = false;
                }
            }
            if (!Shockwave) {
                Falling = false;
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource) {
            if (ModContent.GetInstance<tsorcRevampConfig>().DeleteDroppedSoulsOnDeath) {
                for (int i = 0; i < 400; i++) {
                    if (Main.item[i].type == ModContent.ItemType<DarkSoul>()) {
                        Main.item[i].active = false;
                    }
                }
            }
            return true;
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
            Projectile.NewProjectile(player.Bottom, new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Bloodsign>(), 0, 0, player.whoAmI);
            Main.PlaySound(SoundID.NPCDeath58.WithVolume(0.8f).WithPitchVariance(.3f), player.position);
        }
        public override void UpdateDead() {
            if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath) {
                souldroptimer++;
                if (souldroptimer == 5 && souldroplooptimer < 13) {
                    foreach (Item item in player.inventory) {
                        if (item.type == ModContent.ItemType<DarkSoul>() && Main.netMode != NetmodeID.MultiplayerClient) {
                            Item.NewItem(player.Center, item.type, item.stack);
                            souldroplooptimer++;
                            souldroptimer = 0;
                            item.stack = 0;
                        }
                    }
                }
            }
            DarkInferno = false;
            Falling = false;
        }

        public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit) {
            if (MeleeArmorVamp10) {
                if (Main.rand.Next(10) == 0) {
                    player.HealEffect(damage / 10);
                    player.statLife += (damage / 10);
                }
            }
            if (NUVamp) {
                if (Main.rand.Next(5) == 0) {
                    player.HealEffect(damage / 4);
                    player.statLife += (damage / 4);
                }
            }

        }
        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit) {
            if (OldWeapon) {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
        }
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (OldWeapon) {
                float damageMult = Main.rand.NextFloat(0.0f, 0.8696f);
                damage = (int)(damage * damageMult);
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit) {
            int NT = npc.type;
            if (DragonStone) {
                if (NT == 2 || NT == 6 || NT == 34 || NT == 42 || NT == 48 || NT == 49 || NT == 51 || NT == 60 || NT == 61 || NT == 62 || NT == 66 || NT == 75 || NT == 87 || NT == 88 || NT == 89 || NT == 90 || NT == 91 || NT == 92 || NT == 93 || NT == 94 || NT == 112 || NT == 122 || NT == 133 || NT == 137
                    || NT == NPCID.Probe
                    || NT == NPCID.IceBat
                    || NT == NPCID.Lavabat
                    || NT == NPCID.GiantFlyingFox
                    || NT == NPCID.RedDevil
                    || NT == NPCID.VampireBat
                    || NT == NPCID.IceElemental
                    || NT == NPCID.PigronCorruption
                    || NT == NPCID.PigronHallow
                    || NT == NPCID.PigronCrimson
                    || NT == NPCID.Crimera
                    || NT == NPCID.MossHornet
                    || NT == NPCID.CrimsonAxe
                    || NT == NPCID.FloatyGross
                    || NT == NPCID.Moth
                    || NT == NPCID.Bee
                    || NT == NPCID.FlyingFish
                    || NT == NPCID.FlyingSnake
                    || NT == NPCID.AngryNimbus
                    || NT == NPCID.Parrot
                    || NT == NPCID.Reaper
                    || NT == NPCID.IchorSticker
                    || NT == NPCID.DungeonSpirit
                    || NT == NPCID.Ghost
                    || NT == NPCID.ElfCopter
                    || NT == NPCID.Flocko
                    || NT == NPCID.MartianDrone
                    || NT == NPCID.MartianProbe
                    || NT == NPCID.ShadowFlameApparition
                    || NT == NPCID.MothronSpawn
                    || NT == NPCID.GraniteFlyer
                    || NT == NPCID.FlyingAntlion
                    || NT == NPCID.DesertDjinn
                    || NT == NPCID.SandElemental) {
                    damage = 0;
                }
            }
            if (UndeadTalisman) {
                if (NPCID.Sets.Skeletons.Contains(npc.type)
                    || npc.type == NPCID.Zombie
                    || npc.type == NPCID.Skeleton
                    || npc.type == NPCID.BaldZombie
                    || npc.type == NPCID.AngryBones
                    || npc.type == NPCID.ArmoredViking
                    || npc.type == NPCID.UndeadViking
                    || npc.type == NPCID.DarkCaster
                    || npc.type == NPCID.CursedSkull
                    || npc.type == NPCID.UndeadMiner
                    || npc.type == NPCID.Tim
                    || npc.type == NPCID.DoctorBones
                    || npc.type == NPCID.ArmoredSkeleton
                    || npc.type == NPCID.Mummy
                    || npc.type == NPCID.DarkMummy
                    || npc.type == NPCID.LightMummy
                    || npc.type == NPCID.Wraith
                    || npc.type == NPCID.SkeletonArcher
                    || npc.type == NPCID.PossessedArmor
                    || npc.type == NPCID.TheGroom
                    || npc.type == NPCID.SkeletronHand
                    || npc.type == NPCID.SkeletronHead
                    || npc.type == ModContent.NPCType<NPCs.Bosses.GravelordNito>()
                    /* || NT == mod.NPCType("MagmaSkeleton") || NT == mod.NPCType("Troll") || NT == mod.NPCType("HeavyZombie") || NT == mod.NPCType("IceSkeleton") || NT == mod.NPCType("IrateBones")*/) {
                    damage -= 15;

                    if (damage < 0) damage = 0;
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BoneRevenge) {
                if (!Main.hardMode) {
                    for (int b = 0; b < 8; b++) {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3f, 3f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 20, 4f, 0, 0, 0);
                    }
                }
                else {
                    for (int b = 0; b < 12; b++) {
                        Projectile.NewProjectile(player.position, new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), -4), ModContent.ProjectileType<Projectiles.BoneRevenge>(), 40, 5f, 0, 0, 0);
                    }
                }
            }
        }

        public override void UpdateBadLifeRegen() {
            if (DarkInferno) {
                if (player.lifeRegen > 0) {
                    player.lifeRegen = 0;
                }
                player.lifeRegenTime = 0;
                player.lifeRegen = -11;
                for (int j = 0; j < 4; j++) {
                    int dust = Dust.NewDust(player.position, player.width / 2, player.height / 2, 54, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(player.position, player.width / 2, player.height / 2, 58, (player.velocity.X * 0.2f), player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (tsorcRevamp.toggleDragoonBoots.JustPressed) {
                DragoonBootsEnable = !DragoonBootsEnable;
            }
        }

        public override void PreUpdate() {
            if (DragoonBoots && DragoonBootsEnable) { //lets do this the smart way
                Player.jumpSpeed += 10f;

            }
        }
    }
}
