using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Melee;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.HolyLance
{
	public class HolyLanceProj : ModdedSpearProjectile
	{
		public override float HoldoutRangeMin => 105f;
		public override float HoldoutRangeMax => 210f;
		public override float HitboxSize => 1;
		public override float Scale => 1f;
		public override int dustID => DustID.MagicMirror;
	}
}
