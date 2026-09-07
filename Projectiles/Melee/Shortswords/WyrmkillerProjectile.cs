using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.NPCs.Bosses.JungleWyvern;
using tsorcRevamp.NPCs.Bosses.Okiku.SecondForm;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath;
using tsorcRevamp.NPCs.Bosses.WyvernMage;

namespace tsorcRevamp.Projectiles.Melee.Shortswords
{
    // Shortsword projectiles are handled in a special way with how they draw and damage things
    // The "hitbox" itself is closer to the player, the sprite is centered on it
    // However the interactions with the world will occur offset from this hitbox, closer to the sword's tip (CutTiles, Colliding)
    // Values chosen mostly correspond to Iron Shortword
    public class WyrmkillerProjectile : ModdedShortswordProj
    {
        public override float HitboxWidth => 10f;
        public override float HitboxLength => 6f;

        public override int SpriteWidth => 50;
        public override int SpriteHeight => 50;
        public override int TotalDuration => 16;
        
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            //what a mess lmao, should probably be a switch but im lazy
            if (target.type == NPCID.WyvernBody
                || target.type == NPCID.WyvernBody2
                || target.type == NPCID.WyvernBody3
                || target.type == NPCID.WyvernHead
                || target.type == NPCID.WyvernTail
                || target.type == ModContent.NPCType<ShadowDragonBody>()
                || target.type == ModContent.NPCType<ShadowDragonHead>()
                || target.type == ModContent.NPCType<MechaDragonBody>()
                || target.type == ModContent.NPCType<MechaDragonBody2>()
                || target.type == ModContent.NPCType<MechaDragonBody3>()
                || target.type == ModContent.NPCType<MechaDragonHead>()
                || target.type == ModContent.NPCType<MechaDragonLegs>()
                || target.type == ModContent.NPCType<MechaDragonTail>()
                || target.type == ModContent.NPCType<JungleWyvernBody>()
                || target.type == ModContent.NPCType<JungleWyvernBody2>()
                || target.type == ModContent.NPCType<JungleWyvernBody3>()
                || target.type == ModContent.NPCType<JungleWyvernHead>()
                || target.type == ModContent.NPCType<JungleWyvernLegs>()
                || target.type == ModContent.NPCType<JungleWyvernTail>()
                || target.type == ModContent.NPCType<GhostDragonBody>()
                || target.type == ModContent.NPCType<GhostDragonBody2>()
                || target.type == ModContent.NPCType<GhostDragonBody3>()
                || target.type == ModContent.NPCType<GhostDragonHead>()
                || target.type == ModContent.NPCType<GhostDragonLegs>()
                || target.type == ModContent.NPCType<GhostDragonTail>()
                || target.type == ModContent.NPCType<HellkiteDragonBody>()
                || target.type == ModContent.NPCType<HellkiteDragonBody2>()
                || target.type == ModContent.NPCType<HellkiteDragonBody3>()
                || target.type == ModContent.NPCType<HellkiteDragonHead>()
                || target.type == ModContent.NPCType<HellkiteDragonLegs>()
                || target.type == ModContent.NPCType<HellkiteDragonTail>()
                || target.type == ModContent.NPCType<SeathTheScalelessBody>()
                || target.type == ModContent.NPCType<SeathTheScalelessBody2>()
                || target.type == ModContent.NPCType<SeathTheScalelessBody3>()
                || target.type == ModContent.NPCType<SeathTheScalelessHead>()
                || target.type == ModContent.NPCType<SeathTheScalelessLegs>()
                || target.type == ModContent.NPCType<SeathTheScalelessTail>()
                )
            {
                modifiers.FinalDamage *= Wyrmkiller.DmgMult;
            }
        }
    }
}
