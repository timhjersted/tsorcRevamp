using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.NPCs.FriendlyPuppets;

namespace tsorcRevamp.Content.Items.Weapons.Summon
{
    /// <summary>
    /// Summons a Soul Mimic (the MimicTear NPC — NPCs/FriendlyPuppets/MimicTear.cs). No recipe — it is a drop.
    ///
    /// Unlike the Spirit Bells this spawns no minion projectile: the mimic is an NPC, its minion slots are reserved
    /// by MirrorPuppetOwnerPlayer, its lifetime is <see cref="MimicTearBuff"/> and its cooldown is
    /// <see cref="MimicTearCooldown"/>. Damage is 0 on purpose: the mimic's damage comes from the weapons it copies,
    /// and a damage of 0 keeps the Spirit Ashes summon-weapon rules (SpiritAshesItem.IsSummonWeapon) off it.
    /// </summary>
    public class MimicRune : ModItem
    {
        // PLACEHOLDER sprite (copy of ArcherSpiritBell) — replace with bespoke art.

        // {0} minion slots, {1} minutes it lasts, {2} minutes of cooldown.
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MimicTear.MinionSlotCost, MimicTear.LifetimeSeconds / 60, MimicTear.ResummonCooldownSeconds / 60);

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
            Item.UseSound = SoundID.Item4; // Mana-crystal chime: a rune being invoked
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<MimicTearCooldown>()))
            {
                return false;
            }

            // Needs at least one minion slot, and room for the slots it will take (all of them, up to MinionSlotCost).
            // Counted from the live minions rather than player.slotsMinions, which vanilla has just zeroed at this
            // point in the update. Refusing here follows the Spirit Bells: no evicting unrelated minions.
            float slotsNeeded = System.Math.Min(MimicTear.MinionSlotCost, player.maxMinions);
            float slotsInUse = 0f;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];

                if (projectile.active && projectile.owner == player.whoAmI && projectile.minion)
                {
                    slotsInUse += projectile.minionSlots;
                }
            }

            bool hasRoom = player.maxMinions >= 1 && slotsInUse + slotsNeeded <= player.maxMinions;
            return hasRoom;
        }

        public override bool? UseItem(Player player)
        {
            // UseItem runs for every client's copy of this player; only the owner summons.
            if (player.whoAmI != Main.myPlayer)
            {
                return true;
            }

            player.AddBuff(ModContent.BuffType<MimicTearBuff>(), MimicTear.SummonBuffGraceTicks);
            player.AddBuff(ModContent.BuffType<MimicTearCooldown>(), MimicTear.ResummonCooldownSeconds * 60);
            MimicTear.RequestSpawn(player);
            return true;
        }
    }
}
