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

        public bool MeleeArmorVamp10 = false;
        public bool NUVamp = false;

        public bool OldWeapon = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;

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
            if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath) {
                foreach (Item item in player.inventory) {
                    if (item.type == ModContent.ItemType<DarkSoul>() && Main.netMode != NetmodeID.MultiplayerClient) {
                        Item.NewItem(player.Center, item.type, item.stack);
                        item.stack = 0;
                    }
                }
            }
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
                //todo make the player take no damage from flying enemies
            }
            if (UndeadTalisman) {
                if (NPCID.Sets.Skeletons.Contains(npc.type)
                    || npc.type == NPCID.Zombie
                    || npc.type == NPCID.BaldZombie
                    || npc.type == NPCID.AngryBones
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
        public override void UpdateDead() {
            DarkInferno = false;

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
