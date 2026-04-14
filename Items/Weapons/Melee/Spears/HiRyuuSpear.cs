using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class HiRyuuSpear : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<HiRyuuSpearProj>();
        public override int Width => 64;
        public override int Height => 64;
        public override int BaseDmg => 190;
        public override int BaseCritChance => 16;
        public override float BaseKnockback => 7;
        public override int UseAnimationTime => 16;
        public override int UseTime => 16;
        public override int Rarity => ItemRarityID.Red;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public const float BonusDmgWhileFalling = 120f;
        public const int HealOnHit = 3;
        public static float HiRyuuSpearDamageBoost = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BonusDmgWhileFalling, HealOnHit, HiRyuuSpearDamageBoost);
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0)
            {
                damage *= BonusDmgWhileFalling / 100f;
            }
        }
    }
}
