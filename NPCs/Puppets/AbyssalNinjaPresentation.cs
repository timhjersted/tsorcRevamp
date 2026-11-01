using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.NPCs.Puppets
{
    public partial class AbyssalNinja
    {
        private static Effect _auraEffect;
        protected override bool ShowWeaponDuringNeutral => true;
        public override void PostAI()
        {
            base.PostAI();
            if (!EncounterActive || _fieldAge < 60) return;
            // Run on server AND each player's peer from the same synced NPC/anchor.
            // AddBuff preserves a longer lease from another source; never ClearBuff on exit.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && Vector2.DistanceSquared(player.Center, _fieldAnchor) < 800 * 800)
                    player.AddBuff(ModContent.BuffType<Suppressed>(), 15, true);
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (EncounterActive) DrawField(spriteBatch, screenPos);
            if (_stage == Stage.NightVanish) return false;
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (_withdrawing || Phase != AttackPhase.Custom) return;
            int elapsed = Math.Max(0, _stageTicks - PhaseTimer);
            if (_stage == Stage.MeridianTell && elapsed >= 32)
                DrawTellLine(spriteBatch, _origin, _destination, screenPos, AbyssColor * 0.5f, 2);
            if ((_stage == Stage.NeedleTell || _stage == Stage.CrossbowTell) && elapsed >= _stageTicks - 12)
            {
                Vector2 muzzle = PuppetHandPosition + (_move == Move.Crossbow ? new Vector2(2 * _facing, -2) + _aim * 35 : Vector2.Zero);
                Color cue = _move == Move.Crossbow ? (_pattern == 0 ? Color.White : _pattern == 1 ? Color.Yellow : Color.Red) : AbyssColor;
                DrawTellLine(spriteBatch, muzzle, muzzle + _aim * 700, screenPos, cue * 0.45f, 2);
                if (_move == Move.Crossbow)
                    for (int i = 0; i <= _pattern; i++)
                        DrawTellLine(spriteBatch, muzzle + new Vector2(i * 6 - 6, -12), muzzle + new Vector2(i * 6 - 6, -18), screenPos, cue * 0.8f, 2);
            }
            bool nightMarked = (_stage == Stage.NightTell && elapsed >= 20) || _stage == Stage.NightVanish
                || _stage == Stage.NightAppear || _stage == Stage.NightHold;
            if (nightMarked)
            {
                DrawTellLine(spriteBatch, _destination, _landing, screenPos, AbyssColor * 0.5f, 2);
                Vector2 ground = _landing + Vector2.UnitY * (NPC.height / 2f);
                DrawTellLine(spriteBatch, ground - Vector2.UnitX * 28, ground + Vector2.UnitX * 28, screenPos, Color.Silver * 0.65f, 2);
            }
            if (_customLive)
            {
                Vector2 hand = PuppetHandPosition, tip = PuppetWeaponTipPosition(80);
                ModifyFrontBladeCapsule(ref hand, ref tip);
                DrawTellLine(spriteBatch, hand, tip, screenPos, new Color(210, 175, 255) * 0.6f, 2);
            }
        }
        private static void DrawTellLine(SpriteBatch batch, Vector2 from, Vector2 to, Vector2 screen, Color color, int width)
        {
            Vector2 d = to - from;
            if (d.LengthSquared() < 1) return;
            int count = Math.Max(1, (int)(d.Length() / 12));
            for (int i = 0; i < count; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)count);
                batch.Draw(TextureAssets.MagicPixel.Value, p - screen, null, color, d.ToRotation(),
                    new Vector2(0, 0.5f), new Vector2(Math.Min(8, d.Length()), width), SpriteEffects.None, 0);
            }
        }
        private void DrawField(SpriteBatch batch, Vector2 screen)
        {
            _auraEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", AssetRequestMode.ImmediateLoad).Value;
            if (tsorcRevamp.NoiseWavy == null) return;
            float formation = MathHelper.Clamp(_fieldAge / 60f, 0, 1);
            int size = (int)(1600 / 0.7f);
            Rectangle source = new Rectangle(0, 0, size, size);
            batch.End();
            batch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            _auraEffect.Parameters["textureSize"]?.SetValue((float)tsorcRevamp.NoiseWavy.Width);
            _auraEffect.Parameters["effectSize"]?.SetValue(source.Size());
            _auraEffect.Parameters["effectColor"]?.SetValue((AbyssColor * 0.35f).ToVector4());
            _auraEffect.Parameters["ringProgress"]?.SetValue(formation);
            _auraEffect.Parameters["fadePercent"]?.SetValue(0f);
            _auraEffect.Parameters["time"]?.SetValue(Main.GlobalTimeWrappedHourly * 0.8f);
            _auraEffect.Parameters["scaleFactor"]?.SetValue(2.5f);
            _auraEffect.CurrentTechnique.Passes[0].Apply();
            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, _fieldAnchor - screen, source, Color.White, 0,
                source.Size() / 2f, 1, SpriteEffects.None, 0);
            UsefulFunctions.RestartSpritebatch(ref batch);
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_move); writer.Write((byte)_stage); writer.Write((short)_stageTicks);
            writer.Write((byte)_tier); writer.Write((byte)_pendingTier); writer.Write(_encounterId); writer.Write((byte)_fieldAge);
            WriteVector(writer, _fieldAnchor); writer.Write(_withdrawing);
            writer.Write((sbyte)_facing); writer.Write((short)_lockedTarget);
            writer.Write(_poseRotation); writer.Write(_bodySpin);
            WriteVector(writer, _aim); WriteVector(writer, _origin); WriteVector(writer, _destination);
            WriteVector(writer, _landing); WriteVector(writer, _launch);
            writer.Write((short)_strikeAt); writer.Write((byte)_shotsFired); writer.Write((short)_nextShot);
            writer.Write((byte)_pattern); writer.Write((byte)_passes); writer.Write(_nightCut);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Stage previousStage = _stage;
            base.ReceiveExtraAI(reader);
            _move = (Move)reader.ReadByte(); _stage = (Stage)reader.ReadByte(); _stageTicks = reader.ReadInt16();
            _tier = reader.ReadByte(); _pendingTier = reader.ReadByte(); _encounterId = reader.ReadInt32(); _fieldAge = reader.ReadByte();
            _fieldAnchor = ReadVector(reader); _withdrawing = reader.ReadBoolean();
            _facing = reader.ReadSByte(); _lockedTarget = reader.ReadInt16();
            _poseRotation = reader.ReadSingle(); _bodySpin = reader.ReadSingle();
            _aim = ReadVector(reader); _origin = ReadVector(reader); _destination = ReadVector(reader);
            _landing = ReadVector(reader); _launch = ReadVector(reader);
            _strikeAt = reader.ReadInt16(); _shotsFired = reader.ReadByte(); _nextShot = reader.ReadInt16();
            _pattern = reader.ReadByte(); _passes = reader.ReadByte(); _nightCut = reader.ReadBoolean();
            if (previousStage != _stage && (_stage == Stage.BackAir || _stage == Stage.EclipseAir || _stage == Stage.NightDive))
                _landingEffectPlayed = false;
            if (Phase == AttackPhase.Custom && (_stage == Stage.CrossbowReady || _stage == Stage.CrossbowTell || _stage == Stage.CrossbowFire))
                ConfigureCustomRangedWeapon(SecondaryRangedWeaponItemType, RangedStyle.Crossbow);
        }
        private static void WriteVector(BinaryWriter writer, Vector2 vector) { writer.Write(vector.X); writer.Write(vector.Y); }
        private static Vector2 ReadVector(BinaryReader reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
}
