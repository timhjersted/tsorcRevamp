using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;

namespace tsorcRevamp.Players
{
    public class CursedDragonArmorPlayer : ModPlayer
    {
        public const int BreathManaCost = 2;
        public const int BreathUseRate = 5;
        public const int BreathDamage = 48;
        public const float StaminaRegenBonus = 0.05f;

        public bool CursedDragonSet;
        public bool CursedDragonVisuals;
        private int breathTimer;

        public override void ResetEffects()
        {
            CursedDragonSet = false;
            CursedDragonVisuals = false;
        }

        public override void FrameEffects()
        {
            ApplyDragonVisuals();
        }

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            ApplyDragonVisuals();
        }

        private void ApplyDragonVisuals()
        {
            if (!CursedDragonVisuals && !Player.HasBuff(ModContent.BuffType<DragonForm>()))
            {
                return;
            }

            int headSlot = EquipLoader.GetEquipSlot(Mod, "CursedDragonHelmet", EquipType.Head);
            int bodySlot = EquipLoader.GetEquipSlot(Mod, "CursedDragonArmor", EquipType.Body);
            int legsSlot = EquipLoader.GetEquipSlot(Mod, "CursedDragonGreaves", EquipType.Legs);

            if (headSlot >= 0)
            {
                Player.head = headSlot;
            }
            if (bodySlot >= 0)
            {
                Player.body = bodySlot;
            }
            if (legsSlot >= 0)
            {
                Player.legs = legsSlot;
            }
        }

        public override void PostUpdateEquips()
        {
            if (!CursedDragonSet)
            {
                return;
            }

            ApplyMasterNinjaGearEffects();

            Player.buffImmune[BuffID.OnFire] = true;
            Player.buffImmune[BuffID.OnFire3] = true;
            Player.buffImmune[BuffID.Burning] = true;
            Player.fireWalk = true;
            Player.lavaRose = true;

            Player.jumpBoost = true;
            Player.jumpSpeedBoost += 1.1f;
            Player.jumpHeight += 8;

            Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegenBonus;

            ApplyTatteredDragonWings();
        }

        public override void PostUpdate()
        {
            if (!CursedDragonSet || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (CanBreatheFire())
            {
                breathTimer++;
                Player.direction = Main.MouseWorld.X >= Player.Center.X ? 1 : -1;

                if (breathTimer >= BreathUseRate)
                {
                    breathTimer = 0;
                    BreatheFire();
                }
            }
            else
            {
                breathTimer = BreathUseRate;
            }
        }

        private void ApplyMasterNinjaGearEffects()
        {
            Player.blackBelt = true;
            Player.dashType = 1;
            Player.spikedBoots = 2;
        }

        private void ApplyTatteredDragonWings()
        {
            Item wings = ContentSamples.ItemsByType[ItemID.TatteredFairyWings];
            int wingSlot = wings.wingSlot;
            if (wingSlot <= 0)
            {
                return;
            }

            Player.wings = wingSlot;
            if (HasFunctionalWingsEquipped())
            {
                return;
            }

            Player.wingsLogic = wingSlot;
            Player.wingTimeMax = 180;
            Player.rocketTimeMax = 180;
        }

        private bool HasFunctionalWingsEquipped()
        {
            int slots = 8 + Player.extraAccessorySlots;
            for (int i = 3; i < slots; i++)
            {
                Item item = Player.armor[i];
                if (item != null && !item.IsAir && item.wingSlot > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanBreatheFire()
        {
            if (Main.playerInventory || Player.dead || Player.CCed || Player.noItems || Player.mouseInterface)
            {
                return false;
            }
            if (!Player.controlUseTile)
            {
                return false;
            }
            if (Player.HeldItem != null && !Player.HeldItem.IsAir && ItemLoader.AltFunctionUse(Player.HeldItem, Player))
            {
                return false;
            }

            return Player.statMana >= GetBreathManaCost();
        }

        private int GetBreathManaCost()
        {
            int cost = (int)(BreathManaCost * Player.manaCost);
            return cost < 1 ? 1 : cost;
        }

        private void BreatheFire()
        {
            int cost = GetBreathManaCost();
            if (Player.statMana < cost)
            {
                return;
            }

            Player.GetModPlayer<tsorcRevampPlayer>().SpendManaOnHit(cost);

            Vector2 mouth = Player.MountedCenter + new Vector2(Player.direction * 16f, -12f);
            Vector2 aim = (Main.MouseWorld - mouth).SafeNormalize(new Vector2(Player.direction, 0f));
            if (Vector2.Dot(aim, new Vector2(Player.direction, 0f)) < 0.25f)
            {
                aim = new Vector2(Player.direction, aim.Y * 0.35f).SafeNormalize(new Vector2(Player.direction, 0f));
            }

            IEntitySource source = Player.GetSource_Misc("CursedDragonBreath");
            Vector2 velocity = aim.RotatedByRandom(0.12f) * Main.rand.NextFloat(7.5f, 9.5f);
            Projectile.NewProjectile(source, mouth, velocity, ProjectileID.Flames, BreathDamage, 1f, Player.whoAmI);

            if (Main.rand.NextBool(3))
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.35f, Pitch = 0.25f }, Player.Center);
            }
        }
    }
}