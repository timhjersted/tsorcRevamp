using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Which composite arm a <see cref="PuppetHand"/> drives. Front renders over the torso, Back
    /// renders under it — the only real difference vanilla draws between the two arms.
    /// </summary>
    public enum PuppetHandSlot : byte
    {
        Front,
        Back,
    }

    /// <summary>
    /// Data-only description of one weapon as a puppet wields it. Everything here has to travel with
    /// the weapon rather than the puppet, because a hand can swap weapons between attacks (sword in
    /// one combo, mace in the next) and the grip point, sprite scale, and blade thickness all change
    /// with it.
    ///
    /// The -1 / NaN sentinels mean "inherit the puppet's own value" so a weapon can leave a field
    /// unspecified and still resolve to exactly what the old per-puppet virtual returned. That is
    /// what keeps the default front-hand weapon a byte-identical stand-in for the legacy properties.
    /// </summary>
    public sealed class PuppetWeapon
    {
        /// <summary>Item type whose sprite is drawn in the hand. Negative = this hand is empty.</summary>
        public int ItemType { get; }

        /// <summary>Contact damage for this weapon's swings. Negative = inherit the puppet's MeleeDamage.</summary>
        public int Damage { get; }

        /// <summary>Normalised (0-1) grip point within the weapon texture — see MeleeHandleNorm.</summary>
        public Vector2 HandleNorm { get; }

        public float DrawScale { get; }

        /// <summary>Thickness (px) of the tracked blade capsule used for hit detection.</summary>
        public float BladeWidth { get; }

        public WeaponArchetype Archetype { get; }

        /// <summary>Base swing reach in px. Negative = inherit the puppet's ComboReachBase.</summary>
        public float ReachBase { get; }

        /// <summary>Draw-only angular correction (radians) for sprites that don't follow the
        /// broadsword diagonal convention (handle lower-left, tip upper-right).</summary>
        public float RotationOffset { get; }

        /// <summary>True for asymmetric heads (an axe) that must mirror on reversed arcs so the
        /// cutting edge leads rather than trails.</summary>
        public bool SingleBladed { get; }

        /// <summary>True when the weapon's real visual is a separate projectile (a flail's ball and
        /// chain, a whip's segments), so the item icon must not be drawn in the hand.</summary>
        public bool HideHeldSprite { get; }

        public PuppetWeapon(
            int itemType,
            int damage = -1,
            Vector2? handleNorm = null,
            float drawScale = 1f,
            float bladeWidth = 48f,
            WeaponArchetype archetype = WeaponArchetype.None,
            float reachBase = -1f,
            float rotationOffset = 0f,
            bool singleBladed = false,
            bool hideHeldSprite = false)
        {
            ItemType = itemType;
            Damage = damage;
            HandleNorm = handleNorm ?? new Vector2(0.10f, 0.85f);
            DrawScale = drawScale;
            BladeWidth = bladeWidth;
            Archetype = archetype;
            ReachBase = reachBase;
            RotationOffset = rotationOffset;
            SingleBladed = singleBladed;
            HideHeldSprite = hideHeldSprite;
        }

        /// <summary>The empty hand. Held by the back hand of every puppet that hasn't opted into a
        /// second weapon, so hand state is never null and callers never need a null check.</summary>
        public static readonly PuppetWeapon None = new PuppetWeapon(itemType: -1);

        public bool IsEmpty => ItemType < 0;
    }

    /// <summary>
    /// Live per-hand state: which weapon this hand currently holds, plus the swing clock and blade
    /// tracking that belong to THIS hand's motion. Two of these per puppet (front and back) is what
    /// lets each arm swing on its own timeline instead of sharing one set of fields.
    ///
    /// Deliberately a passive state bag — PuppetNPC owns all the behavior that reads and writes it,
    /// so there is exactly one place to breakpoint a swing.
    /// </summary>
    public sealed class PuppetHand
    {
        public PuppetHand(PuppetHandSlot slot)
        {
            Slot = slot;
            Weapon = PuppetWeapon.None;
        }

        public PuppetHandSlot Slot { get; }

        /// <summary>The weapon in this hand right now. Swapped per attack via PuppetNPC.EquipWeapon;
        /// never null (an empty hand holds <see cref="PuppetWeapon.None"/>).</summary>
        public PuppetWeapon Weapon { get; set; }

        /// <summary>Swing angle (radians) this hand's arm and weapon sprite are posed at.</summary>
        public float Rotation { get; set; }

        /// <summary>Countdown of the current swing animation, and the value it started from. Drives
        /// the arm pose rows the same way the legacy single-hand fields did.</summary>
        public int Anim { get; set; }
        public int AnimMax { get; set; }

        /// <summary>This hand's own clip clock. Separate instances are the whole point: the front
        /// hand can be mid-recovery while the back hand is still winding up.</summary>
        public PuppetAttackRuntime Runtime { get; } = new PuppetAttackRuntime();

        // ── Tracked blade collision (per hand, so two weapons can be live at once) ──
        /// <summary>True while this hand's swing should be tested against players each tick.</summary>
        public bool BladeArmed { get; set; }
        public float BladeReach { get; set; }
        public int BladeDamage { get; set; }
        public float BladeKnockback { get; set; }

        /// <summary>Previous tick's capsule endpoints, used to sweep between ticks so a fast swing
        /// can't tunnel past a stationary player. Invalid until <see cref="HasPreviousBladeSample"/>.</summary>
        public bool HasPreviousBladeSample { get; set; }
        public Vector2 PreviousBladeOrigin { get; set; }
        public Vector2 PreviousBladeTip { get; set; }

        /// <summary>Player indices already struck by the current swing — one hit per swing per hand.</summary>
        public HashSet<int> BladeHitPlayers { get; } = new HashSet<int>();
    }
}
