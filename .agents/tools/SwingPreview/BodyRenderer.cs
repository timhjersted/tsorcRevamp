using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using tsorcRevamp.NPCs.Puppets;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace SwingPreview
{
    /// <summary>One frame's worth of pose, produced by the simulation and consumed by the renderer.</summary>
    internal sealed class PoseFrame
    {
        public int Tick;
        public string Phase;
        public bool Armed;
        public bool Airborne;
        public float WeaponRotation;      // radians, the mod's swing-space angle
        public float CompositeArmRotation; // radians, vanilla composite-arm space
        public int Direction = 1;
        public int BodyRow;
        public int LegRow;
    }

    /// <summary>Which sprite sheets to composite, and the handful of numbers the pose maths needs.</summary>
    internal sealed class PuppetArt
    {
        public string Name;
        public string BodySheet;
        public string LegsSheet;
        public string HeadSheet;
        public string WeaponSprite;
        public float WeaponRotationOffset;   // MeleeWeaponRotationOffset
        public float DrawScale = 1f;

        /// <summary>Weapon sprite scale. CALIBRATION KNOB, not derived: PuppetNPC.DrawWeaponToLayer
        /// computes its own draw size from grip points and reach, and that is not reproduced here.
        /// Dial it with --weaponscale until the blade matches a screenshot, or read the real value
        /// out of a telemetry log's "scale" / "visualReach" fields.</summary>
        public float WeaponScale = 0.45f;
    }

    /// <summary>
    /// Composites a puppet's real sprite sheets frame by frame, offline.
    ///
    /// Every transform here is vanilla's, taken from the decompiled draw layers:
    ///   composite cells are 40x56 (CreateCompositeFrameRect: pt.X*40, pt.Y*56)
    ///   front arm rotates about bodyVect + (-5, 0)  =  (15, 28) in the cell
    ///   back arm  rotates about bodyVect + ( 6, 2)  =  (26, 30)
    ///   torso cell is column 0 (column 1 on the jump frame), male rows 0/1, female rows 2/3
    /// Layer order matches PlayerDrawLayers: legs -> back arm -> torso -> head -> weapon -> front
    /// arm + shoulder cap.
    ///
    /// FIDELITY: body, arm and pivot placement are vanilla maths and should match the game. The
    /// WEAPON anchor is an approximation - PuppetNPC.DrawWeaponToLayer carries grip-point, origin and
    /// flip logic that is not reproduced here, so treat the blade's exact hand contact as indicative.
    /// Skin is not drawn at all: vanilla player skin ships as packed .xnb, and Gwyn's armour sets
    /// HidesTopSkin/HidesArms/HidesHands anyway.
    /// </summary>
    internal static class BodyRenderer
    {
        private const int CellW = 40;
        private const int CellH = 56;

        // Cell-space rotation pivots. Vanilla flips the offset sign with the sprite so the SAME art
        // texel stays the pivot in both facings, which is why these are facing-independent here.
        private static readonly PointF FrontArmPivot = new PointF(15f, 28f);
        private static readonly PointF BackArmPivot = new PointF(26f, 30f);

        // Composite sheet columns.
        private const int ColTorso = 0;
        private const int ColTorsoJump = 1;
        private const int ColFrontShoulder = 0;
        private const int ColBackShoulder = 1;
        private const int ColFrontArm = 7;
        private const int ColBackArm = 8;

        // Male rows for torso / shoulder. Female is +2; every puppet is Male (PuppetNPC.InitPuppet).
        private const int RowTorso = 0;
        private const int RowShoulder = 1;

        internal static void Render(PuppetArt art, List<PoseFrame> frames, string outDir, string label, int zoom)
        {
            Directory.CreateDirectory(outDir);

            using Bitmap body = Load(art.BodySheet);
            using Bitmap legs = Load(art.LegsSheet);
            using Bitmap head = Load(art.HeadSheet);
            using Bitmap weapon = Load(art.WeaponSprite);

            var rendered = new List<Bitmap>();
            foreach (PoseFrame frame in frames)
            {
                rendered.Add(DrawFrame(art, frame, body, legs, head, weapon, zoom));
            }

            string safe = Sanitize(label);
            string stripPath = Path.Combine(outDir, $"body-{Sanitize(art.Name)}-{safe}-sheet.png");
            string sheetPath = Path.Combine(outDir, $"body-{Sanitize(art.Name)}-{safe}-contact.png");
            string htmlPath = Path.Combine(outDir, $"body-{Sanitize(art.Name)}-{safe}.html");

            int sheetColumns = SaveAnimationSheet(rendered, stripPath);
            SaveContactSheet(rendered, frames, sheetPath);

            // Inlined as a data URI rather than referenced: the player is meant to be opened, sent
            // and downloaded on its own, and a relative <img> reference silently produces an empty
            // stage the moment the two files are separated - which looks like a broken page, not a
            // missing file.
            string sheetDataUri = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(stripPath));
            SaveHtmlPlayer(rendered, frames, sheetDataUri, htmlPath, art.Name, label, sheetColumns);

            foreach (Bitmap b in rendered) { b.Dispose(); }

            Console.WriteLine($"    strip   -> {stripPath}");
            Console.WriteLine($"    contact -> {sheetPath}");
            Console.WriteLine($"    player  -> {htmlPath}");
        }

        private static Bitmap Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"sprite not found: {path}");
            }
            // Copy out so the file handle is not held for the life of the render.
            using var loaded = new Bitmap(path);
            return new Bitmap(loaded);
        }

        private static Bitmap DrawFrame(PuppetArt art, PoseFrame frame,
            Bitmap body, Bitmap legs, Bitmap head, Bitmap weapon, int zoom)
        {
            // Generous canvas: a greatsword at full extension reaches well past the 40x56 body cell.
            const int PadX = 60;
            const int PadY = 50;
            int w = (CellW + PadX * 2) * zoom;
            int h = (CellH + PadY * 2) * zoom;

            var canvas = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(canvas);
            g.Clear(Color.FromArgb(255, 30, 32, 40));

            // Nearest-neighbour: this is pixel art, and smoothing would hide exactly the 1-2px
            // registration errors the preview exists to expose.
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;

            g.ScaleTransform(zoom, zoom);
            g.TranslateTransform(PadX, PadY);

            int torsoColumn = frame.Airborne ? ColTorsoJump : ColTorso;
            Rectangle torsoCell = Cell(torsoColumn, RowTorso);
            Rectangle frontShoulderCell = Cell(ColFrontShoulder, RowShoulder);
            Rectangle backShoulderCell = Cell(ColBackShoulder, RowShoulder);
            int stretchRow = StretchRow();
            Rectangle frontArmCell = Cell(ColFrontArm, stretchRow);
            Rectangle backArmCell = Cell(ColBackArm, stretchRow);

            Rectangle legFrame = LegacyFrame(frame.LegRow);
            Rectangle headFrame = LegacyFrame(frame.BodyRow);

            bool flip = frame.Direction < 0;

            // ---- vanilla layer order ----
            DrawStatic(g, legs, legFrame, flip);
            DrawRotated(g, body, backArmCell, BackArmPivot, frame.CompositeArmRotation * 0.55f, flip);
            DrawStatic(g, body, backShoulderCell, flip);
            DrawStatic(g, body, torsoCell, flip);
            DrawStatic(g, head, headFrame, flip);
            DrawWeapon(g, weapon, art, frame, flip);
            DrawRotated(g, body, frontArmCell, FrontArmPivot, frame.CompositeArmRotation, flip);
            DrawStatic(g, body, frontShoulderCell, flip);

            g.ResetTransform();
            DrawLabel(canvas, frame);
            return canvas;
        }

        /// <summary>Composite arm stretch, as a sheet row. UpdateCompositeArm maps Full 0,
        /// ThreeQuarters 1, Quarter 2, None 3. PuppetNPC.CompositeArmStretch is internal to the mod
        /// assembly and is a runtime /swingarm tunable rather than gameplay config, so the preview
        /// carries its own copy defaulting to the same value the mod ships (Full).</summary>
        internal static int ArmStretch = 0;

        private static readonly float[] StretchRadius = { 10f, 8f, 6f, 4f };

        private static int StretchRow() => ArmStretch;

        private static Rectangle Cell(int column, int row) => new Rectangle(column * CellW, row * CellH, CellW, CellH);

        private static Rectangle LegacyFrame(int row) => new Rectangle(0, row * CellH, CellW, CellH);

        private static void DrawStatic(Graphics g, Bitmap sheet, Rectangle source, bool flip)
        {
            if (source.Bottom > sheet.Height || source.Right > sheet.Width) { return; }
            DrawCell(g, sheet, source, new PointF(0f, 0f), PointF.Empty, 0f, flip);
        }

        private static void DrawRotated(Graphics g, Bitmap sheet, Rectangle source, PointF pivot, float radians, bool flip)
        {
            if (source.Bottom > sheet.Height || source.Right > sheet.Width) { return; }
            DrawCell(g, sheet, source, new PointF(0f, 0f), pivot, radians, flip);
        }

        /// <summary>Blits one 40x56 cell, optionally mirrored and rotated about a cell-space pivot.
        /// Mirroring flips about the cell's horizontal centre, which is what SpriteEffects does.</summary>
        private static void DrawCell(Graphics g, Bitmap sheet, Rectangle source,
            PointF at, PointF pivot, float radians, bool flip)
        {
            GraphicsState state = g.Save();

            if (Math.Abs(radians) > 0.0001f)
            {
                g.TranslateTransform(at.X + pivot.X, at.Y + pivot.Y);
                g.RotateTransform((float)(radians * 180.0 / Math.PI) * (flip ? -1f : 1f));
                g.TranslateTransform(-(at.X + pivot.X), -(at.Y + pivot.Y));
            }

            if (flip)
            {
                g.TranslateTransform(at.X + CellW / 2f, 0f);
                g.ScaleTransform(-1f, 1f);
                g.TranslateTransform(-(at.X + CellW / 2f), 0f);
            }

            g.DrawImage(sheet, new Rectangle((int)at.X, (int)at.Y, CellW, CellH), source, GraphicsUnit.Pixel);
            g.Restore(state);
        }

        /// <summary>
        /// Places the weapon at the composite front hand and rotates it along the blade.
        /// APPROXIMATE - see the fidelity note on the class. The hand comes from vanilla's
        /// GetFrontHandPosition maths relative to the cell, and the sprite is pinned by its lower-left
        /// (the hilt corner for a Terraria sword), which is the convention the real draw starts from.
        /// </summary>
        private static void DrawWeapon(Graphics g, Bitmap weapon, PuppetArt art, PoseFrame frame, bool flip)
        {
            PointF hand = FrontHandInCell(frame.CompositeArmRotation, flip);
            float degrees = (float)((frame.WeaponRotation + art.WeaponRotationOffset * frame.Direction) * 180.0 / Math.PI);

            GraphicsState state = g.Save();
            g.TranslateTransform(hand.X, hand.Y);

            if (flip)
            {
                g.ScaleTransform(-1f, 1f);
                degrees = -degrees;
            }

            g.RotateTransform(degrees);

            // Terraria sword sprites run hilt at bottom-left to tip at top-right, which is the
            // MeleeNaturalRestAngleDeg = 45 pose. So rotation 0 needs no correction: pin the sprite's
            // bottom-left corner to the hand and rotate the whole thing about that point.
            float w = weapon.Width * art.WeaponScale;
            float h = weapon.Height * art.WeaponScale;
            g.DrawImage(weapon, new RectangleF(0f, -h, w, h),
                new RectangleF(0f, 0f, weapon.Width, weapon.Height), GraphicsUnit.Pixel);

            g.Restore(state);
        }

        /// <summary>Vanilla Player.GetFrontHandPosition, expressed in cell space rather than world
        /// space: MountedCenter maps to the cell centre (20, 28).</summary>
        private static PointF FrontHandInCell(float rotation, bool flip)
        {
            float num = rotation + (float)Math.PI / 2f;
            var unit = new PointF((float)Math.Cos(num), (float)Math.Sin(num));

            float radius = StretchRadius[Math.Clamp(ArmStretch, 0, 3)];

            float x = unit.X * radius;
            float y = unit.Y * radius;

            // The direction-dependent shoulder offset plus the rotated (0, +-3) wrist nudge.
            float sign = flip ? -1f : 1f;
            x += -4f * sign;
            y += -2f;
            x += -(3f * sign) * (float)Math.Sin(num);
            y += (3f * sign) * (float)Math.Cos(num);

            return new PointF(20f + x, 28f + y);
        }

        private static void DrawLabel(Bitmap canvas, PoseFrame frame)
        {
            using Graphics g = Graphics.FromImage(canvas);
            using var font = new Font("Consolas", 9f);
            Color tint = frame.Armed ? Color.FromArgb(255, 235, 90, 90) : Color.FromArgb(255, 150, 155, 165);
            using var brush = new SolidBrush(tint);
            string air = frame.Airborne ? " air" : "";
            g.DrawString($"t{frame.Tick} {ShortPhase(frame.Phase)}{air}", font, brush, 4, 4);
        }

        private static string ShortPhase(string phase)
        {
            if (string.IsNullOrEmpty(phase)) { return "?"; }
            if (phase.Contains("Telegraph")) { return "windup"; }
            if (phase.Contains("Recovery")) { return "recovery"; }
            return "swing";
        }

        /// <summary>
        /// Packs the frames into a GRID sheet and returns the column count.
        ///
        /// Deliberately not a single row: a 107-frame swing at zoom 4 is ~64,600px wide, and browsers
        /// refuse to decode an image past roughly 32,767px on an axis. The failure is silent - the
        /// player's controls all work and the sprite is simply never painted, which reads as a bug in
        /// the page rather than in the image. Squaring the sheet keeps both axes small.
        /// </summary>
        private static int SaveAnimationSheet(List<Bitmap> frames, string path)
        {
            if (frames.Count == 0) { return 1; }

            int fw = frames[0].Width;
            int fh = frames[0].Height;
            int columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(frames.Count)));
            int rows = (int)Math.Ceiling(frames.Count / (double)columns);

            using var sheet = new Bitmap(fw * columns, fh * rows, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(sheet))
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    g.DrawImageUnscaled(frames[i], (i % columns) * fw, (i / columns) * fh);
                }
            }
            sheet.Save(path, ImageFormat.Png);

            Console.WriteLine($"    sheet   -> {sheet.Width}x{sheet.Height}px, {columns} cols x {rows} rows");
            return columns;
        }

        /// <summary>Onion-skin grid. Sub-samples to keep the sheet readable on long swings.</summary>
        private static void SaveContactSheet(List<Bitmap> frames, List<PoseFrame> poses, string path)
        {
            if (frames.Count == 0) { return; }

            int step = Math.Max(1, (int)Math.Ceiling(frames.Count / 24.0));
            var picked = new List<int>();
            for (int i = 0; i < frames.Count; i += step) { picked.Add(i); }

            int cols = Math.Min(6, picked.Count);
            int rows = (int)Math.Ceiling(picked.Count / (double)cols);
            int fw = frames[0].Width;
            int fh = frames[0].Height;

            using var sheet = new Bitmap(fw * cols, fh * rows, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(sheet))
            {
                g.Clear(Color.FromArgb(255, 18, 20, 26));
                for (int i = 0; i < picked.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    g.DrawImageUnscaled(frames[picked[i]], col * fw, row * fh);
                }
            }
            sheet.Save(path, ImageFormat.Png);
        }

        /// <summary>
        /// Self-contained scrubber: play/pause, frame step, and a speed control down to 0.1x, which
        /// is what makes a handoff snap or a one-frame pose pop actually visible.
        /// </summary>
        private static void SaveHtmlPlayer(List<Bitmap> frames, List<PoseFrame> poses,
            string stripFileName, string path, string puppet, string label, int sheetColumns)
        {
            if (frames.Count == 0) { return; }
            int fw = frames[0].Width;
            int fh = frames[0].Height;

            var phases = new StringBuilder();
            for (int i = 0; i < poses.Count; i++)
            {
                if (i > 0) { phases.Append(','); }
                string tag = ShortPhase(poses[i].Phase) + (poses[i].Armed ? "*" : "") + (poses[i].Airborne ? " air" : "");
                phases.Append('"').Append(tag).Append('"');
            }

            string html = $@"<!doctype html>
<meta charset=""utf-8"">
<title>{puppet} / {label}</title>
<style>
  body {{ background:#16181e; color:#e6e8ee; font:13px Consolas,monospace; margin:0; padding:16px; }}
  h1 {{ font-size:15px; margin:0 0 12px; font-weight:600; }}
  #stage {{ width:{fw}px; height:{fh}px; background-image:url('{stripFileName}');
            background-repeat:no-repeat; image-rendering:pixelated; border:1px solid #3a3f4b; }}
  .row {{ display:flex; align-items:center; gap:10px; margin-top:12px; }}
  input[type=range] {{ width:420px; }}
  button {{ background:#2a2f3a; color:#e6e8ee; border:1px solid #444b59; padding:5px 12px;
            font:13px Consolas,monospace; cursor:pointer; border-radius:3px; }}
  button:hover {{ background:#353b48; }}
  #meta {{ color:#9aa1ae; }}
  .swing {{ color:#f06a6a; }}
</style>
<h1>{puppet} &nbsp;/&nbsp; {label} &nbsp;<span id=""meta"">{frames.Count} frames @60fps</span></h1>
<div id=""stage""></div>
<div class=""row"">
  <button id=""play"">pause</button>
  <button id=""prev"">&#9664; step</button>
  <button id=""next"">step &#9654;</button>
  <input id=""scrub"" type=""range"" min=""0"" max=""{frames.Count - 1}"" value=""0"">
  <span id=""label""></span>
</div>
<div class=""row"">
  <span>speed</span>
  <input id=""speed"" type=""range"" min=""1"" max=""100"" value=""100"">
  <span id=""speedLabel"">1.00x</span>
</div>
<script>
  var FW = {fw}, FH = {fh}, COLS = {sheetColumns}, N = {frames.Count};
  var phases = [{phases}];
  var stage = document.getElementById('stage'), scrub = document.getElementById('scrub');
  var label = document.getElementById('label'), speed = document.getElementById('speed');
  var speedLabel = document.getElementById('speedLabel'), playBtn = document.getElementById('play');
  var frame = 0, playing = true, acc = 0, last = performance.now();

  function show(i) {{
    frame = (i + N) % N;
    stage.style.backgroundPosition =
      (-(frame % COLS) * FW) + 'px ' + (-Math.floor(frame / COLS) * FH) + 'px';
    scrub.value = frame;
    var p = phases[frame] || '';
    label.innerHTML = 'frame ' + frame + ' / ' + (N - 1) + ' &nbsp; ' +
      (p.indexOf('*') >= 0 ? '<span class=""swing"">' + p + '</span>' : p);
  }}

  function loop(now) {{
    var dt = now - last; last = now;
    if (playing) {{
      acc += dt * (speed.value / 100);
      while (acc >= 1000 / 60) {{ acc -= 1000 / 60; show(frame + 1); }}
    }}
    requestAnimationFrame(loop);
  }}

  playBtn.onclick = function () {{ playing = !playing; playBtn.textContent = playing ? 'pause' : 'play'; }};
  document.getElementById('prev').onclick = function () {{ playing = false; playBtn.textContent = 'play'; show(frame - 1); }};
  document.getElementById('next').onclick = function () {{ playing = false; playBtn.textContent = 'play'; show(frame + 1); }};
  scrub.oninput = function () {{ playing = false; playBtn.textContent = 'play'; show(+scrub.value); }};
  speed.oninput = function () {{ speedLabel.textContent = (speed.value / 100).toFixed(2) + 'x'; }};

  show(0);
  requestAnimationFrame(loop);
</script>";

            File.WriteAllText(path, html);
        }

        private static string Sanitize(string value)
        {
            var sb = new StringBuilder();
            foreach (char c in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c)) { sb.Append(c); }
            }
            return sb.Length == 0 ? "swing" : sb.ToString();
        }
    }
}
