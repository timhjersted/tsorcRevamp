using Terraria;
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

        public int CurseLevel = 1;

        public bool MeleeArmorVamp10 = false;

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

        }

        public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit)
        {
            if(MeleeArmorVamp10)
            {
                if(Main.rand.Next(2) == 0)
                {
                    player.HealEffect(damage / 10);
                    player.statLife += (damage / 10);
                }
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit) {
            int NT = npc.type;
            if (DragonStone) {
                //todo make the player take no damage from flying enemies
            }
            if (UndeadTalisman) {
                if (NT == 3 || NT == 21 || NT == 31 || NT == 32 || NT == 33 || NT == 34 || NT == 44 || NT == 45 || NT == 52 || NT == 53 || NT == 77 || NT == 78 || NT == 79 || NT == 80 || NT == 82 || NT == 109 || NT == 110 || NT == 132 || NT == 140/* || NT == mod.NPCType("MagmaSkeleton") || NT == mod.NPCType("Troll") || NT == mod.NPCType("HeavyZombie") || NT == mod.NPCType("IceSkeleton") || NT == mod.NPCType("IrateBones")*/) {
                    damage -= 15;

                    if (damage < 0) damage = 0;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (tsorcRevamp.toggleDragoonBoots.JustPressed) {
                DragoonBootsEnable = !DragoonBootsEnable;
            }
        }
        public override void PreUpdate() {
            if (DragoonBoots) {
                if (DragoonBootsEnable) {
                    Player.jumpSpeed += 10f;
                }
            }
        }

    }
}
