---
name: vfx-shader-tips
description: Supplemental guide on best practices, HLSL techniques, frame UV normalization, noise blending, edge detection, and shader architecture for Terraria tModLoader VFX. Includes an offline CPU preview harness (preview/) for seeing a shader before launching the game.
---

# 🎨 Supplemental VFX Shader Tips & HLSL Best Practices for Terraria (tModLoader)

This skill provides advanced principles, pipelines, and mathematical techniques for authoring high-quality 2D HLSL (`.fx`) shaders in Terraria. It draws on lessons learned from large open-source mod repositories (e.g., Calamity, Spirit Mod) and Reach `ps_2_0` shader architecture.

> **Start here if you are about to write or fix an `.fx` file:** §42 (render it offline before you
> ship it — `preview/`) and §43 (additive cannot make a saturated colour over a bright sky). Those two
> account for most of the "it compiled but it looks terrible" time this repo has spent.
>
> - §1–30 — general HLSL technique.
> - §31–41 — field notes: how effects here actually broke, in game.
> - §42–48 — look at it before you ship it: the preview harness and what it immediately exposed.
> - §49–50 — ring-space is for rims not bodies; cheap swirl and the slot savings that actually land.
>
> §43 additionally carries the **failure signature** for forgetting to premultiply on an
> alpha-blended technique: a flat tinted rectangle exactly the size of the draw quad. That one
> shipped to playtest twice; learn to recognise it before you debug the shape maths.

---

## 1. Frame UV Normalization (`uSourceRect`)

### The Problem
Terraria packs sprites and animation frames into large texture sheets or atlases. Standard `TEXCOORD0` (`c`) passed by `SpriteBatch` represents atlas UV coordinates (e.g. `y = 0.25` to `0.50`), NOT `0.0` to `1.0`. Applying noise scaling, radial falloffs, or distance math directly to atlas UVs creates rectangular bounding box artifacts, distorted texture lookups, and visible seams across frame boundaries.

### The Solution
Always calculate and pass a normalized `uSourceRect` vector4 (`float4(left, top, width, height)` in 0..1 atlas coordinates) from C# into the shader uniform parameters:

```csharp
Vector4 uSourceRect = sourceRectangle.HasValue
    ? new Vector4(
        sourceRectangle.Value.X / (float)primaryTexture.Width,
        sourceRectangle.Value.Y / (float)primaryTexture.Height,
        sourceRectangle.Value.Width / (float)primaryTexture.Width,
        sourceRectangle.Value.Height / (float)primaryTexture.Height)
    : new Vector4(0f, 0f, 1f, 1f);
effect.Parameters["uSourceRect"]?.SetValue(uSourceRect);
```

Inside HLSL, convert atlas coordinates (`c`) into frame-local UV coordinates (`0.0` to `1.0`):
```hlsl
float2 frameUV = (c - uSourceRect.xy) / max(uSourceRect.zw, float2(0.001, 0.001));
```

---

## 2. Multi-Layer Noise Blending (Eliminating Seams & Grid Lines)

### The Problem
Sampling a single panning noise texture produces unnatural scrolling and visible repeating grid lines when the noise texture wraps.

### The Solution
Blend **two independent noise samples** panning at different speeds, angles, and scales:
```hlsl
float2 noiseUV1 = frameUV * float2(2.5, 1.8) + float2(-Time * 0.35, Time * 0.05);
float2 noiseUV2 = frameUV * float2(4.2, 3.0) + float2(-Time * 0.50, Time * 0.12);
float n1 = tex2D(DetailSampler, noiseUV1).r;
float n2 = tex2D(DetailSampler, noiseUV2).r;

float turbulence = saturate(n1 * 1.3 + n2 * 0.7 - 0.4);
float body = pow(turbulence, 1.4);
```
Applying non-linear power curves (`pow(..., 1.4)`) creates organic, fluid flame and liquid motion without repeating lines.

---

## 3. Sprite Silhouette Masking & Edge Detection (Gwyn Rim Glow)

### The Problem
Quad shaders drawn over NPCs or projectiles look like floating rectangular boxes rather than attaching to the character's body.

### The Solution
1. Sample primary sprite frame alpha: `float mask = tex2D(PrimarySampler, c).a`.
2. Detect exact sprite silhouette outlines by sampling texel neighbors:
```hlsl
float2 texel = 1.0 / max(PrimaryTextureSize, float2(1.0, 1.0));
float neighborAlpha = min(
    min(tex2D(PrimarySampler, c + float2(texel.x, 0.0)).a,
        tex2D(PrimarySampler, c - float2(texel.x, 0.0)).a),
    min(tex2D(PrimarySampler, c + float2(0.0, texel.y)).a,
        tex2D(PrimarySampler, c - float2(0.0, texel.y)).a));
float edge = saturate((mask - neighborAlpha) * 4.0);
```
3. Multiply output alpha by `mask * Opacity` and add `edge * CoreColor` to produce a radiant glowing rim highlight strictly along the contours of the sprite.

---

## 4. Polar Coordinate Conversion (Vortices, Black Holes & Swirls)

Convert Cartesian frame UVs `(x, y)` into polar coordinates `(angle, radius)` for swirling vortex energy:
```hlsl
float2 p = frameUV - 0.5;
float radius = length(p);
float angle = atan2(p.y, p.x) / 6.283185 + 0.5; // Normalized angle 0..1
float2 polarUV = float2(angle * 2.0 + Time * 0.2, radius * 3.0 - Time * 0.4);
float swirlNoise = tex2D(DetailSampler, polarUV).r;
```
**Best Used For**: Black holes, aquatic whirlpools, radial spell circles, and expanding aura rings.

---

## 5. Alpha Noise Erosion & Glowing Burn Edges (Dissolve Effects)

Instead of a hard `clip()` (which causes aliased pixels), compare noise against a `Progress` parameter with a soft glowing burn edge band along the dissolve threshold:
```hlsl
float noise = tex2D(DetailSampler, frameUV * 2.5).r;
float edgeWidth = 0.08;

float alpha = saturate((noise - Progress) / edgeWidth);
float burnEdge = saturate(1.0 - abs(noise - Progress) / edgeWidth) * (1.0 - Progress);

float3 finalColor = lerp(baseColor, BurnColor, burnEdge);
return float4(finalColor, alpha * baseAlpha);
```
**Best Used For**: Boss death disintegrations, projectile fade-outs, and phase transition dissolves.

---

## 6. Procedural Electrical Arcs & Lightning

Jitter a center line using high-speed scrolling noise:
```hlsl
float noise = tex2D(DetailSampler, float2(frameUV.x * 4.0 + Time * 12.0, 0.5)).r;
float jitter = (noise - 0.5) * ArcJitterAmount;
float distanceToLine = abs(frameUV.y - 0.5 - jitter);
float coreArc = saturate(1.0 - distanceToLine * 18.0);
float outerGlow = saturate(1.0 - distanceToLine * 4.0);

float3 color = lerp(DarkColor, MidColor, outerGlow);
color = lerp(color, CoreColor, coreArc);
return float4(color * (coreArc * 2.0 + outerGlow), outerGlow * Opacity);
```
**Best Used For**: Lightning strikes, electric sparks, plasma beams, and void tendrils.

---

## 7. Chromatic Aberration (RGB Channel Split)

Sample the texture three times with tiny opposite coordinate offsets for the Red and Blue channels:
```hlsl
float2 splitOffset = DirectionVector * AberrationAmount;
float r = tex2D(PrimarySampler, c - splitOffset).r;
float g = tex2D(PrimarySampler, c).g;
float b = tex2D(PrimarySampler, c + splitOffset).b;
float a = tex2D(PrimarySampler, c).a;
return float4(r, g, b, a) * Opacity;
```
**Best Used For**: Heavy impact hits, reality distortion, high-energy explosions, and abyssal teleportation.

---

## 8. 1D Palette Swap Lookup (Dynamic Re-Coloring)

Convert original sprite pixel brightness into a 1D coordinate to sample a color gradient palette texture:
```hlsl
float4 sprite = tex2D(PrimarySampler, c);
float luminance = dot(sprite.rgb, float3(0.299, 0.587, 0.114));
float4 paletteColor = tex2D(PaletteSampler, float2(luminance, 0.5));

float3 finalColor = lerp(sprite.rgb, paletteColor.rgb, uProgress);
return float4(finalColor, sprite.a) * Opacity;
```
**Best Used For**: Re-coloring enemy elemental variants (fire, ice, toxic, dark void) using a single base sprite sheet.

---

## 9. Emissive Glow Masking (Night/Cave Full Brightness)

Use a secondary mask (or specific texture channel) so that eyes, weapon runes, and gems remain 100% full-bright even in pitch-black caves:
```hlsl
float4 sprite = tex2D(PrimarySampler, c);
float emissiveMask = tex2D(EmissiveSampler, c).r; // 1 = full bright glow, 0 = affected by light

float3 litColor = sprite.rgb * EnvironmentalLightColor;
float3 finalColor = lerp(litColor, sprite.rgb * 1.4, emissiveMask);
return float4(finalColor, sprite.a) * Opacity;
```
**Best Used For**: Glowing boss eyes, magical staff gems, glowing armor runes, and dark Souls ambient effects.

---

## 10. 2D Signed Distance Fields (SDF Procedural Shapes)

Calculate analytical geometric distance fields inside the pixel shader for crisp rings, hexagons, and star shapes:
```hlsl
// Circle SDF: distance from center minus radius
float dist = length(frameUV - 0.5) - 0.35;
float edgeWidth = 0.01;
float shapeAlpha = smoothstep(edgeWidth, -edgeWidth, dist);
float outline = smoothstep(0.04, 0.02, abs(dist) - 0.02);
```
**Best Used For**: Sharp spell glyphs, expanding shockwave rings, and forcefield geometry.

---

## 11. Multi-Sample 2D Motion Blur

Sample the primary texture multiple times along a velocity direction vector:
```hlsl
float2 blurVector = DirectionVector * MotionBlurStrength;
float4 col = 0;
for (int i = 0; i < 4; i++) {
    float2 offset = blurVector * (float(i) / 3.0 - 0.5);
    col += tex2D(PrimarySampler, c + offset);
}
return col * 0.25 * Opacity;
```
**Best Used For**: High-speed dash maneuvers, weapon swings, and fast-moving boss projectiles.

---

## 12. Dynamic HSV Hue Shift Cycling

Convert RGB color to HSV, shift the hue dynamically over `Time`, and convert back to RGB for rainbow energy and phase shifts:
```hlsl
float3 rgb2hsv(float3 c) {
    float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + 1e-10)), d / (q.x + 1e-10), q.x);
}
float3 hsv2rgb(float3 c) {
    float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}
```
**Best Used For**: Rainbow shimmer, elemental phase shifts, and enchanted weapon glows.

---

## 13. Luminance Threshold Extraction (Bloom Pre-Pass)

Isolate high-brightness pixels using a luminance threshold before applying additive blurring:
```hlsl
float3 color = tex2D(PrimarySampler, c).rgb;
float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
float3 bloomCore = color * saturate(luma - BloomThreshold) * BloomIntensity;
```
**Best Used For**: Post-processing bloom, intense laser core extractions, and explosion flash masks.

---

## 14. Primitive Trail & Ribbon Shading (`VertexStrip`)

When drawing trail strips using C# `VertexStrip` or `CustomVertexInfo`, `coords.x` maps progress along the trail (0 at head, 1 at tail) and `coords.y` maps across width (0 at top, 1 at bottom):
```hlsl
float progressAlongTrail = coords.x;
float widthAcrossTrail = abs(coords.y - 0.5) * 2.0;
float tailFade = saturate(1.0 - progressAlongTrail);
float edgeSoftness = saturate(1.0 - pow(widthAcrossTrail, 2.0));

float noise = tex2D(DetailSampler, float2(progressAlongTrail * 3.0 - Time * 0.5, coords.y)).r;
float alpha = tailFade * edgeSoftness * saturate(noise + 0.4) * Opacity;
```
**Best Used For**: Sword slashes, laser beams, projectile motion ribbons, and magic tendrils.

---

## 15. Procedural Voronoi Caustics (Water, Magic Barriers & Energy Lattices)

Calculate procedural cellular distance (Voronoi noise) directly inside the pixel shader to create moving aquatic caustics, forcefield hexagonal grids, and magic barrier lattices:
```hlsl
float2 st = frameUV * 4.0;
float2 i_st = floor(st);
float2 f_st = frac(st);
float m_dist = 1.0;

for (int y = -1; y <= 1; y++) {
    for (int x = -1; x <= 1; x++) {
        float2 neighbor = float2(x, y);
        float2 p = frac(sin(float2(dot(i_st + neighbor, float2(127.1, 311.7)), 
                                   dot(i_st + neighbor, float2(269.5, 183.3)))) * 43758.5);
        p = 0.5 + 0.5 * sin(Time * 2.5 + 6.2831 * p);
        float2 diff = neighbor + p - f_st;
        m_dist = min(m_dist, length(diff));
    }
}
float causticIntensity = pow(1.0 - m_dist, 2.5);
```
**Best Used For**: Water caustics, magic barriers, forcefields, dragon scale shimmers, and crystalline energy.

---

## 16. 2D Fresnel Rim Lighting (3D Depth Effect on Sprites)

Calculate a radial gradient or normal-distance falloff from the sprite center to simulate glowing 3D-like rim lighting along the edges:
```hlsl
float centerDist = length(frameUV - 0.5) * 2.0;
float fresnelRim = pow(saturate(centerDist), 3.0) * spriteAlpha;

float3 finalColor = spriteColor.rgb + RimColor.rgb * fresnelRim * RimIntensity;
return float4(finalColor, spriteAlpha) * Opacity;
```
**Best Used For**: Giving 2D boss sprites and large weapons a volumetric, 3D lit appearance in low-light environments.

---

## 17. Animated Trail Width & Tapering (`VertexStrip` Geometry)

Animate the width of primitive trail quads in HLSL by multiplying width across `coords.x` (trail length) with a bell curve equation:
```hlsl
float trailProgress = coords.x; // 0 at head, 1 at tail
float widthWidth = abs(coords.y - 0.5) * 2.0;

// Bell-curve width: starts thin, expands to peak width, then tapers to sharp tail point
float widthProfile = sin(trailProgress * 3.14159); 
float alpha = saturate((1.0 - widthWidth / max(widthProfile, 0.01))) * (1.0 - trailProgress);
```
**Best Used For**: Sword slashes, scythe arcs, projectile ribbons, and dynamic energy whips.

---

## 18. Screen-Space Refraction & Water Distortion

Use world position and screen UVs (`screenUV = (worldPosition - Main.screenPosition) / ScreenSize`) to sample the background game screen buffer and distort background tiles behind moving water waves or shockwaves:
```hlsl
float2 rippleOffset = (tex2D(NoiseSampler, frameUV * 2.0 + float2(Time * 0.2, 0.0)).rg - 0.5) * 0.015;
float4 distortedBackground = tex2D(ScreenSampler, screenUV + rippleOffset);
```
**Best Used For**: Tidal crest waves, underwater distortion bubbles, and explosion shockwaves.

---

## 19. Texture Channel Packing (Optimization for Reach `ps_2_0`)

Pack three separate noise/mask textures into a single RGBA image file:
- **Red Channel**: Micro detail noise (sparks, foam, sharp caustics)
- **Green Channel**: Macro smooth turbulence (body flow)
- **Blue Channel**: Distance field mask / vignette shape

This keeps shader instruction slot counts low (< 64 instructions) while sampling multiple noise maps in a single `tex2D` call.

---

## 20. The 3-Layer Particle System Architecture
Never rely on a single particle type. High-end boss and weapon VFX combine 3 distinct particle layers:
- **Background Ambient (Atmosphere)**: Large, low-opacity, slow-moving particles (wide aura glows, smoke, ambient fog). Sets elemental color tone.
- **Midground Motion (Action)**: Medium-sized, directional particles (embers, water droplets, directional sparks) that follow velocity and gravity.
- **Foreground High-Contrast (Sharp Detail)**: Small, fast-moving, white-hot glints/stars and high-contrast sparks. Gives the effect sharp visual crispness.

---

## 21. Telegraphing & Anticipation Design
- **Inward Energy Suction**: Before an explosion or laser fires, spawn particles that move **inward** toward the origin point (suction phase). This signals energy concentration.
- **Color Hierarchy**: Maintain strict mod-wide color rules (bright cyan = water, deep violet = abyss void, neon red = unblockable attack).
- **Telegraph Duration**: Standard boss telegraphs should last 30–60 ticks (0.5s–1.0s), matching the dodge-roll invulnerability window so players can react.

---

## 22. Impact Polish: Hit Stop & Screen Shake Decay
- **Decaying Screen Shake**: Apply camera position offsets in `ModifyScreenPosition` with exponential decay (`shake *= 0.88f`).
- **Hit Stop / Frame Pause**: Pausing the attacker's animation frame for 1–3 ticks upon a massive hit creates physical weight and impact force.
- **Secondary Motion**: Fireballs and laser beams should leave floating lingering sparks and smoke wisps that drift independently after the primary projectile passes.

---

## 23. Easing Curves for Particle Lifespans
Particles and energy rings should never move linearly or pop out instantly:
- **Scale Easing (Pop-In & Shrink)**:
  - First 25% of life: Quick pop-in from scale `0.0` to `1.2`.
  - Remaining 75%: Gradual shrink from `1.2` down to `0.0`.
- **Alpha Easing (Quadratic Fade-Out)**: Fade out using `alpha = pow(1.0 - progress, 2.0)`, making the fade start slow and accelerate as the particle dissipates.

---

## 24. Additive vs. Alpha Blend Layering
- **Additive Blending (`BlendState.Additive`)**: Essential for magic energy, fire, glowing eyes, and laser cores. Overlapping additive layers automatically stack up to form intense white-hot cores.
- **Alpha Blending (`BlendState.AlphaBlend`)**: Used for solid entities, dark ink, smoke, blood, and shadow energy to maintain dark silhouettes against bright backgrounds.

---

## 25. Multi-Resolution Layering
Terraria tiles are constrained to a 16×16 pixel grid. By using sub-pixel particle positioning, smooth primitive strips (`VertexStrip`), and high-resolution shader noise textures scaled down, your mod's visual effects acquire a state-of-the-art, fluid quality that wows players while remaining distinct from the tile environment.

---

## 26. Branchless Math Optimization (Replacing `if` in HLSL)

Avoid using `if` / `else` conditional branching inside pixel shaders (which causes GPU performance degradation or Reach compiler slot errors). Use smooth step and `lerp` functions instead:
```hlsl
// Replace: if (r < radius) color = A; else color = B;
// With:
float t = saturate((radius - r) * Softness);
float3 color = lerp(ColorB, ColorA, t);
```
**Best Used For**: Keeping GPU performance fast and guaranteeing Reach `ps_2_0` instruction slot limits (< 64 instructions) are never exceeded.

---

## 27. Smooth Analytical Radial Falloffs

Avoid hard conditional checks (`if (r > 0.5) discard;`) or rigid step functions. Use smooth power falloff equations:
- **Outer Edge Mask**: `float edgeMask = saturate(pow(1.0 - saturate(r), 1.8));`
- **Soft Ring Bands**: `float ring = saturate(1.0 - abs(r - radius) * sharpness);`

---

## 28. Dynamic 3-Tier Color Ramping

Structure color palettes using 3-tier color lerping driven by noise turbulence and energy intensity:
```hlsl
float3 color = lerp(DarkColor, MidColor, bodyIntensity);
color = lerp(color, CoreColor, peakIntensity + edgeGlow);
```
This generates deep shadow contrast, vibrant mid-tones, and fiery/aquatic white-hot energy cores.

---

## 29. C# Render State Safety

Always encapsulate custom shader calls in safe `try ... finally` blocks to restore textures and sampler states:
```csharp
Main.spriteBatch.End();
Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
try
{
    effect.Parameters["uSourceRect"]?.SetValue(uSourceRect);
    effect.CurrentTechnique.Passes[0].Apply();
    Main.EntitySpriteDraw(...);
}
finally
{
    graphicsDevice.Textures[1] = previousTexture;
    graphicsDevice.SamplerStates[1] = previousSampler;
    UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
}
```

---

## 30. Anatomical Origin Placement

VFX and telegraph sigils should originate at character/weapon attachment points (e.g. `StaffTip = NPC.Center + new Vector2(NPC.direction * 14f, -32f)`) rather than defaulting to `NPC.Center`, guaranteeing visual alignment between sprite animations and shader overlays.

**Measure the attachment point, don't eyeball it.** Scan the sprite for the feature's pixels and take the centroid — guessing produces effects that look subtly detached forever:
```powershell
Add-Type -AssemblyName System.Drawing
$img = New-Object System.Drawing.Bitmap("NPCs\Enemies\EvilEye.png")
# accumulate x/y of pixels matching the feature (e.g. a purple iris), restricted to the
# region that isolates it from same-coloured parts elsewhere on the sheet (a purple mouth)
```
EvilEye's telegraph looked disconnected because the iris sits **22px above** the frame centre — the sprite's centre lands in its mouth. Remember to mirror the horizontal offset when `spriteDirection == -1` flips the sheet, and to add `NPC.gfxOffY`.

---

# Part 2 — Field notes: recurring failure modes

Sections 1–30 are technique. These are the specific ways real effects in this repo turned out to be
broken, each found more than once. Repo-specific compile-pipeline details (`fxc.exe` diagnostics, the
`saturate()`-on-uniform compiler bug, texture-channel traps) live in
`.agents/skills/vfx-pipeline/SKILL.md` — check both before starting.

---

## 31. Accidental Rectangles (the #1 visual bug in this repo)

Straight white edges around an effect have exactly two causes, and both showed up repeatedly:

**A. A square SDF you forgot was square.** `max(abs(p.x), abs(p.y))` is a *box* distance. Banded, it draws a literal rectangle outline:
```hlsl
float box = max(abs(p.x), abs(p.y));
float boundary = Active * saturate((0.045 - abs(box - 0.46)) * 24.0);  // <- a white rectangle
```
Found three times: EvilEye's dash aperture, the DemonSpirit soul burst reused for EvilEye's teleport, and Eland's old toxic field box path. If you want a ring, use `length(p)`.

**B. Alpha that is still non-zero when the quad clips it.** Any noise term that *subtracts* from the effective radius can drag density back above zero right at the boundary, and the square quad then slices it flat:
```hlsl
// radial reaches 0 at r = 0.5 ONLY when billow is neutral; when billow is low it does not
float radial = 1.0 - smoothstep(edge - feather, 0.5, r + (billow - 0.5) * feather * 1.1);
```
The fix is a **noise-independent** cutoff that provably hits zero before the boundary in every direction. Square it for a soft approach, and it doubles as a dome that makes small puffs read as globular instead of as flat discs:
```hlsl
float quadFade = saturate((0.5 - r) * 7.0);
radial *= quadFade * quadFade;
```
**Rule: every radial effect needs a term that guarantees alpha reaches 0 before the quad edge, independent of any noise.** Never rely on the shape math alone.

---

## 32. Ring-Space Sampling (cheaper and more seamless than polar §4)

For anything annular — portals, expanding rings, auras — sampling along the **normalised direction vector** beats `atan2` polar mapping on both counts:
```hlsl
float len = length(p);
float2 dir = p / max(len, 0.0005);
float2 uv1 = dir * (0.42 + r * 0.20) + float2(0.5 + Time * 0.030, 0.5 - Time * 0.024);
float2 uv2 = dir * (0.27 - r * 0.13) + float2(0.5 - Time * 0.021, 0.5 + Time * 0.017);
```
Walking around the effect traces a **circle** through texture space, so it is continuous by construction: no rectangular tiling grid, and none of the `±π` wrap seam that `atan2` reintroduces. It also costs ~4 slots instead of ~14 — the EvilEye portal was **85/64 slots with `atan2` and 62/64 with this**, which was the difference between shipping and not. Use §4's polar form only when you genuinely need angle as a *value* (spokes, sweeps, angular gradients).

---

## 33. Per-Instance Variation Without New Uniforms

Shaders sample noise in **local UV space**, so every instance of an effect samples the identical pattern. A row of trail puffs then reads as one stamp repeated rather than a continuous cloud — no amount of shader tuning fixes it, because the instances are genuinely identical.

You rarely need a new uniform. **Pass a random per-instance rotation into the existing `rotation` draw parameter**; rotating the quad rotates the sampled noise:
```csharp
public override void OnSpawn(IEntitySource source)
{
    if (Projectile.ai[1] == 0f)              // ai[] so it survives MP sync
    {
        Projectile.ai[1] = Main.rand.NextFloat(MathHelper.TwoPi);
        Projectile.netUpdate = true;
    }
}
// ...later: DrawX(center, size, progress, active, rotation: Projectile.ai[1]);
```
Zero shader cost, zero new parameters, and safe for any radially symmetric effect. For sprites that spin anyway, feed `Projectile.rotation` in so the shader churns *with* the sprite instead of sitting frozen behind it.

---

## 34. Size the Quad Against the Sprite It Decorates

A glow smaller than the sprite it sits behind is invisible. EvilEye's projectile core was drawn at **18px behind a 16px sprite** — geometrically it could never be seen. The trail was drawn at 68×22 for a squiggle that needed room to travel.

When a shader is meant to read *around* a sprite, draw it well past the sprite's bounds and give the shader an explicit outer halo term that spills into that margin. When it's meant to sit *behind* one, it still has to be bigger than the silhouette or it contributes nothing. Check the sprite's actual pixel dimensions before picking a draw size.

---

## 35. Find Out Who Else Uses a Technique Before Editing It

Techniques get reused across unrelated enemies. `DemonSpiritSoulBurst` was shared between DemonSpirit's live explosion and EvilEye's teleport — "fixing" its square-border term in place would have silently altered a different enemy's signature effect.

```bash
grep -rn '"TechniqueName"' --include="*.cs" .    # every call site, before you touch the .fx
```
If a second consumer wants different behaviour, **give it its own technique**. Duplicating ~30 lines of HLSL is far cheaper than an invisible regression in an enemy you weren't working on.

---

## 36. `ps_2_0` Slot Economy: What's Actually Free

The 64-arithmetic-slot ceiling is reached constantly. Non-obvious findings from real budget fights:

- **`saturate` is usually free; `max(x, 0.0)` is not.** `saturate` compiles to a modifier on the preceding instruction. Swapping `saturate(a - b)` to `max(a - b, 0.0)` to "save" work *raised* a shader from 65 to 66 slots. Reach for `saturate` first.
- **A helper that divides by a uniform can be pure waste.** The common `UV(c)` helper (`c * PrimaryTextureSize / max(DrawSize, 1)`) is an identity whenever the draw supplies no source rectangle, because the helper sets the `DrawSize` uniform to the *texture* size in that case. Dropping it and using `c` directly freed ~4 slots and got a fog shader under budget. Verify against your `Draw` implementation before assuming.
- **`x * x` beats `pow(x, 2.0)`**, and a cubed term (`d * d * d`) is much cheaper than two `pow` calls when you want a fast-arriving core.
- When over budget, cut a **decorative** term (an extra bloom, a tick-mark ring), never the shape math — a half-drawn silhouette looks broken, a slightly plainer glow does not.

---

## 37. Progress Plumbing: Three Ways Callers Break Shader Animation

The shader is usually fine; the `progress` reaching it is not. All three of these were live:

1. **Divisor doesn't match the lifetime.** `timeLeft = 6*60` with `progress = 1 - timeLeft / 180f` starts progress at **−1**, and a fade curve like `saturate(1 - P*P)` renders the effect fully **invisible for its first 1.5s** — precisely when it should be strongest.
2. **A hardcoded constant.** Passing a literal `0.45f` pins the shader to one frame of its animation forever, so it never fades in or out.
3. **A scaled fraction used as a brightness dial.** `progress * 0.18f` was a hack to dim a telegraph, but it also clamps the shader to the first 18% of its curve — killing every `Progress`-driven behaviour. Dim via `Opacity` or an `Active` flag; let `Progress` mean progress.

**Always cross-check the divisor against `SetDefaults`' `timeLeft`.**

---

## 38. Timing IS Counterplay

A large AoE that simply fades in over a second gives the player nothing to react to. Structure lingering effects as an explicit envelope:

- **Grow** out of the caster to full size (~4s) — the expanding edge is the readable threat, and outrunning it is the counterplay.
- **Hold** at full size (~4s).
- **Dissipate** (~3s), with the hitbox driven by the *animated* radius throughout, not a fixed one.
- **Stop damaging before the visual ends** (e.g. 2s into a 3s fade) so trailing wisps are never a surprise hit. Erring this direction is always right: a visual that outlives its hitbox feels generous, a hitbox that outlives its visual feels like a bug.

Related: lingering elements (poison, fog, rot) want a **hold** curve — snap in, sit flat, fade only at the end — not a decay that starts dropping immediately.

---

## 39. Visual / Hitbox Parity — and When to Stop and Ask

Audit whether an effect's damage matches what it draws. A 92px splat was spawning only a `CanDamage() => false` VFX burst — **purely cosmetic**, over a 14px projectile that had already resolved. The visual had been promising an area of effect that did not exist.

Two honest fixes: add a matching hitbox, or shrink the visual. **Adding damage is a balance change, not a polish task** — surface the finding and let the owner choose rather than quietly making an enemy harder. When you do add one, use a true-circle `Colliding` override so the hitbox corners don't catch players standing outside the visible effect.

Also check dust/emission **gating** when a trail "stops": EvilEye's trail died mid-flight because its sparkle was gated to a seeker variant's first 20 ticks — the shader was blameless.

---

## 40. Verification Discipline

- **Verify per-technique, not per-file.** Counting `tex2D(PrimarySampler` matches *per file* and attributing them to whichever technique used a suspect texture produced a confidently wrong bug count — most `.fx` files hold 2–4 techniques. Open the actual pixel-shader function and confirm the specific sampler use. One false positive (a technique that never sampled the texture at all) and several missed hits came from that shortcut.
- **State findings at the confidence you actually have.** "Six confirmed, one ruled out, three more of a different kind" is worth far more than a round number that collapses under one question.
- **Distinguish real compile errors from the build lock.** With tModLoader open, `dotnet build` fails at *packaging* (`TML003`) — but `csc` has already run. Grep for the specific error class to confirm your code is sound:
```bash
dotnet build tsorcRevamp.csproj 2>&1 | grep -E "error CS[0-9]+"   # empty = C# compiled clean
```
- **Round-trip the toolchain before trusting it.** Compile an unmodified `.fx` and `cmp` the output against the checked-in `.xnb`; byte-identical proves the pipeline before you start blaming your own code.

---

## 41. Debug Readouts Are Part of the VFX Workflow

The above-head attack-name overlay (`DrawPuppetAttackDebug`, DebugMode) is the fastest way to tell which effect you are actually looking at — indispensable when tuning a kit where several attacks share a colour. Two things keep it maintainable:

- **Opt in via an interface (`IDebugAttackLabel`), not an `else if` chain** in the HUD. The chain had reached four branches and would have hit eleven; an interface makes each new enemy a one-line change with no HUD edit. Interface members must be `public` — `internal` will not satisfy the contract.
- **Derive labels from the state enum** (`SunlightSlam` → "Sunlight Slam" via a `Humanize` helper) rather than a parallel switch of display strings, which drifts out of sync the moment someone adds a move. Surface the sub-phase too (`"Flail (Committed)"`, `"Spear Throw (Windup)"`) — telegraph-vs-committed is exactly what you need to see while tuning a telegraph.

---

# Part 3 — Look at it before you ship it

§31–41 are bugs found the hard way, in game, over several playtest rounds. §42–48 are what the
**Gravelord Nito** pass found in about ten minutes by rendering the shaders offline first. Read §42
before writing any new `.fx`; the rest fall out of it.

---

## 42. You Can Preview a Shader Without Launching the Game — Do It

**Two harnesses exist. Use the one that matches the question you're asking; do not build a third.**

| | `preview/` (next to this file) | `tModLoader/ShaderHarness/` (outside the repo) |
|---|---|---|
| Run | `dotnet run` | `node render.js <job>` / `node server.js` |
| Output | PNG | PNG, **plus an interactive WebGL page** |
| Extras | `ContactSheet.ps1` for texture picking (§44) | live sliders for `Progress`/`Opacity`/`Active`/time, and a **premultiply toggle** that reproduces the §43 rectangle bug on demand |
| In version control | yes (ships with the repo) | no (tooling, sits beside `ShaderCompiler/`) |

Reach for `preview/` for a still, a batch of `Progress` values, or a contact sheet. Reach for
`ShaderHarness/` when you are tuning a *curve* and want to drag a slider — watching a value move is
far faster than re-rendering a grid, and it is how the spear-wake shear and the bomb fireball were
dialled in. Both carry the same caveat: they are hand ports of the HLSL and **drift within about
three tweaks**, so re-sync after every `.fx` edit.

You cannot see an `.fx` file. Compiling one only proves it fits in `ps_2_0` — it says nothing about
whether it looks like anything. So the loop has historically been: write HLSL, compile, launch
tModLoader, summon a boss, trigger the attack, squint, repeat. That is minutes per iteration and
it is why shaders in this repo have shipped looking wrong for months at a time.

The alternative is embarrassingly cheap. **Port each pixel-shader function to C# and render it to a
PNG.** The maths is a near copy-paste (the harness names its helpers `sat`, `lerp`, `V2`, `V3`,
`C(r,g,b)` for exactly that reason), the textures are ordinary PNGs, and the blend equations are two
lines. Ten minutes to wire up per boss, then every iteration is a `dotnet run`.

On the Nito pass this caught, in the first render: additive colour clipping to white (§43), a
"cracked stone" texture reading as circuitry, fire shaped like popcorn (§45), a silhouette dissolving
into speckle (§46), and three separate rounds of over-bright colour weights. **None of it was visible
in the HLSL.** All of it would otherwise have been playtest findings.

Four details the harness must get right, because each one silently invalidates the preview:

- **Premultiply the textures** (`.r` returns `R_raw * A`) — tModLoader does. Skip this and a texture
  whose image lives in its alpha channel looks fine in the preview and renders as a flat constant in
  game, which is the single most common shape bug in this repo (§ the `T_Windstreak3` trap).
- **Bilinear, wrapping** sampling, matching `SamplerState.LinearWrap`. Nearest sampling makes
  silhouettes blocky and sends you chasing an artifact that does not exist.
- **The correct blend equations.** XNA `Additive` is `SourceAlpha`/`One` → `dst + rgb * a` (alpha
  scales the contribution — it is *not* `One`/`One`). XNA `AlphaBlend` is `One`/`InverseSourceAlpha`,
  i.e. **premultiplied** → `rgb + dst * (1 - a)`.
- **The real draw size from the call site.** Aspect matters; a technique tuned at 1:1 routinely falls
  apart at the 5:1 its caller actually uses.

Then keep it honest: port the maths verbatim including magic numbers, re-sync after every HLSL edit
(they diverge within about three tweaks), and render at more than one `Progress` value. It is a
preview, not a proof — it says nothing about slot count, draw order, `PostDrawTiles` layering,
lighting, or how the thing reads in motion. **Still ship the playtest watchlist.**

---

## 43. Additive Cannot Make a Saturated Colour Over a Bright Background

The highest-value single finding from previewing, and it explains a cluster of complaints that had
always been treated as separate bugs.

Terraria's daytime sky is roughly `(0.6, 0.75, 0.9)`. Additive blending computes `dst + rgb * a`.
So a "deep violet" `(0.45, 0.23, 0.60)` at any intensity high enough to be *visible* against that sky
lands at `(1.05, 0.98, 1.5)` and **clips to white**. The colour you authored is unreachable. Push
harder and it gets whiter, not more purple.

That is one mechanical cause for all of:

- "wisps on projectiles look like solid blocks of white"
- the death nova rendering as a jagged white hoop
- the sword slash washing out to a pale smear
- (previously, on other bosses) "the effect reads as a flat white disc"

The fix is not a better colour ramp. **Use `BlendState.AlphaBlend`, which in XNA is premultiplied**
(`One` / `InverseSourceAlpha`), and it is strictly more expressive than additive:

```hlsl
float alpha = coverage * Opacity;                       // how much it OCCLUDES
return float4(tint * alpha + emissive * Opacity, alpha); // premultiplied
```

- `rgb < alpha` → the effect occludes. You get genuine black, genuine deep red, genuine dark violet.
- `rgb > alpha` → the effect glows *on top of its own occlusion*, additive-style.

One pass gives you both. This is what makes **black fire** possible at all: the sooty body punches a
dark hole and the embers add light back over it. Eight of Nito's nine techniques moved to
premultiplied alpha and that change alone did more for them than every shape edit combined.

Keep pure additive only for things that are honestly *just light* and are meant to stack — a bloom
flash, a spark burst. Everything with a body — smoke, fog, fire, shadow, a wall of death magic —
wants premultiplied alpha.

**The failure signature when you forget to premultiply.** Switching a technique to `AlphaBlend`
without changing its `return` is a silent, extremely confusing bug, and it shipped to playtest on the
Black Knight kit. Because `dst = src.rgb + dst * (1 - src.a)`, a bare `float4(color, alpha)` adds its
colour at **full strength even where alpha is 0** — so the effect paints a **flat tinted rectangle
exactly the size of the draw quad**, with a uniform interior, regardless of how carefully the alpha
is feathered. The feathering is computed and then discarded by the blend.

Learn the signature: shape bugs produce blobs, bands and spikes; *this* produces a clean rectangle
the size of the quad. If you see a rectangle, check the blend contract before you touch the maths.

```bash
# every alpha-blended technique should premultiply; a bare colour return is the bug
grep -n "return float4(color, " Effects/*.fx     # then check that technique's BlendState at the call site
```

Healthy techniques here already multiply colour by something density-related before returning
(`color * energy`, `color * (body * 0.7 + core)`) — that *is* premultiplication. A bare
`float4(color, alpha)` on an alpha-blended draw is always wrong.

**Corollary: preview over a dark background too.** The failure is symmetric. Premultiplied effects
that look superb against sky can turn muddy in a cave. The harness renders both side by side
precisely because neither background alone tells you the truth.

---

## 44. Pick Textures by Looking, Then Check the Seam

**Tooling: `preview/ContactSheet.ps1`.**

Two passes, and the second is the one everybody skips:

1. **Contact sheet of `R * A / 255`** — what the shader actually sees. Filenames lie. `Streak_03` is
   sparse specks, not a streak. `Lightning` is empty in `R * A`. `MarbleNoise` is mostly *white* with
   thin dark veins, so using it directly as a density term gives you a near-constant.
2. **Re-render the shortlist 2x2 tiled.** Every shader in this repo scrolls its samples, so a texture
   with a visible seam is unusable no matter how good the single tile looks.

Pass 2 eliminated the three most obvious-by-name picks for Nito's fire: `T_LiquidVertical_Wave`,
`T_Wave43` and `T_FirePanningCyl45` are all genuinely flame-shaped, and all three carry a hard
horizontal baseline that strobes past every time you scroll them vertically. The kit ended up on
plain seamless turbulence instead — and it looks like better fire, because the flame read comes from
the shape maths (§45), not from the texture being flame-shaped.

---

## 45. Directional Features Come From Anisotropic Sampling Frequency

If your fire looks like popcorn, or your trail looks like a row of puffs, this is why — and it is a
one-line fix, not a redesign.

Noise features elongate along whichever axis you sample at the **lower** frequency:

```hlsl
// vertical flame tongues: HIGH frequency across the column, LOW frequency up it
float fire = tex2D(DetailSampler, float2(c.x * 3.4 - Time * 0.11, c.y * 1.20 - Time * 0.78)).r;

// horizontal streaks down a trail: LOW along the trail, HIGH across it
float wisp = tex2D(DetailSampler, float2(along * 1.1 - Time * 1.05, c.y * 2.6)).r;
```

Nito's plume was originally `float2(c.x * 1.5, y * 1.3)` and `float2(c.x * 2.9, y * 2.4)` — features
wider than they were tall — and rendered as a column of cauliflower. Swapping the ratio turned it
into fire without touching a single colour weight. The same one-line change fixed the sword wake and
both trails.

Combine it with the **noise-modulated falloff distance** from the vfx-pipeline skill ("Techniques
worth copying") and you rarely need a purpose-shaped texture at all.

---

## 46. Separate the Shape Noise From the Detail Noise

A related failure with the same symptom. If one combined noise field drives both the **silhouette**
and the **texture**, the fine layer chews the outline into confetti and the effect stops reading as
an object:

```hlsl
float fire  = saturate(n1 * 0.80 + n2 * 0.75 - 0.28);   // combined: fine detail for COLOUR
float shape = saturate(n1 * 1.35 - 0.22);               // macro layer ONLY, for the SILHOUETTE
float reach = width * (0.35 + shape * 0.62);            // <- shape, not fire
```

Rule of thumb: **the silhouette comes off the smooth macro layer; the fine layer only textures what
the macro layer already decided exists.**

Two more calibration notes from the same pass:

- **Noise should be the smaller half of a width term.** `reach = lens * (0.30 + churn * 0.50)` let
  the noise dominate and the sword wake broke into disconnected puffs. `lens * (0.62 + churn * 0.26)`
  reads as a blade that is being *disturbed* by noise. Same for trails.
- **Raise the baseline when noise should modulate rather than decide.**
  `saturate(n1 * 0.95 + n2 * 0.60 - 0.30)` is a coin-flip per pixel — it speckles.
  `saturate(n1 * 0.75 + n2 * 0.45 - 0.10)` sits mostly-on and undulates.

---

## 47. Prove the Quad Cutoff in a Comment, With the Algebra

§31 says every radial effect needs a noise-independent cutoff. The Nito pass found the discipline
that actually makes that stick: **write the proof next to the code.**

```hlsl
// reach maxes at 0.30 + 0.50 = 0.80 and is zero at both ends of the blade, so `sheath` provably
// hits zero before every one of the four quad edges no matter what the noise does.
float reach = lens * (0.30 + churn * 0.50);
float sheath = saturate((reach - across) * 5.0);
```

Two things fall out of doing this consistently:

- **You often don't need a separate cutoff term at all.** If every contributing term is already
  bounded by a factor that provably reaches zero, the cutoff is free — it is folded into the shape.
  Nito's death ring dropped its explicit `rim` term this way, worth 3 slots.
- **`saturate((reach - across) * k)` beats `saturate(1.0 - across / reach)`.** Same feathered edge,
  no divide, cheaper — and unlike the division form it degrades gracefully to *zero* when `reach`
  goes to zero, instead of leaving a hairline down the centreline at the quad's ends. Watch for that:
  adding a `+ 0.05` floor to `reach` to "avoid a divide by zero" reintroduces exactly that hairline,
  clipped flat at the quad edge.

When the geometry genuinely leaves no room to feather, **the fix may be in the caller.** Nito's nova
ring had its outer lip within ~3px of the quad boundary; no shader term could soften that. Widening
the caller's padding from `2.4x` to `4.5x` the half-thickness gave the shader 64px to work in, cost
nothing, and left the hitbox untouched.

---

## 48. Colour Weight Calibration

Three numbers that were wrong on every first draft, in the same direction each time:

- **Additive intensity above ~1.0 is a white generator.** If you are multiplying a colour by
  `body * 0.7 + core * 1.45 + spark * 2.0` and then *also* scaling by alpha, the bright regions are
  white before any of your palette matters. Under premultiplied alpha, budget the emissive term to
  roughly the range that will still read as coloured over the brightest background you support.
- **A near-white "core" colour is not a highlight, it is a solvent.** `BoneCore = (226, 231, 210)`
  dissolved every ramp it topped. Real bone is `(198, 192, 168)`. Check the top of every ramp against
  a swatch, and stop promoting the core colour into every technique.
- **Burning/hot regions must occlude too**, or the background mixes through them: deep red at low
  alpha over a blue sky is *salmon*. If a flame reads pastel, raise its alpha, don't raise its
  brightness.

And the counterpart to §33: pale desaturated mid-tones are dangerous under any blend mode. A trail
mid of `(122, 112, 146)` lerped toward a bone highlight *is* white; `(98, 74, 136)` keeps its
identity all the way to the highlight.

---

## 49. Ring-Space Is for RIMS, Not for BODIES

§32 recommends ring-space sampling (`dir = p / len`, sample at `dir * k + 0.5`) and it is right for
anything **annular**. It has one failure mode that is invisible in the HLSL and obvious the moment
you render it:

At a fixed angle, varying the radius walks a **straight line through the texture**; neighbouring
angles walk neighbouring lines. So every feature smears **radially**. A fireball sampled in ring
space comes out as a **starburst of spokes**, not billowing fire — the Black Knight bomb detonation
rendered exactly that way, and it reads as a sparkle/firework rather than an explosion.

- **Rims, halos, expanding shockwaves, auras, portals** → ring-space. Continuity around the circle is
  the point, and radial smear is invisible on a thin band.
- **Fireballs, clouds, explosions, anything with volume** → ordinary UV space. To make it expand,
  contract the sampling scale over time so cells appear to rush outward:
  ```hlsl
  float expand = 1.0 - Progress * 0.45;   // cheaper than 1.0 / (a + Progress * b)
  float n = tex2D(DetailSampler, p * (expand * 2.1) + (0.5 + Time * 0.03)).r;
  ```

A single effect often wants both: ordinary UV for the fireball body, ring-space for the shock rim
riding its edge.

---

## 50. Cheap Swirl, and the Slot Savings That Actually Land

An explicit rotation matrix is enormous in `ps_2_0` — one `sin` plus one `cos` took the Vessel maw
from 61 to **90/64 slots** by itself. For a *noise lookup* you almost never need a real rotation:
sliding the sample point along the ring's tangent is visually interchangeable and costs a few ops.

```hlsl
float2 perp  = float2(-dir.y, dir.x);
float  twist = (1.35 - r) * 0.45 + Time * 0.22;   // more twist toward the centre = a vortex
float2 uv    = dir * (0.30 + r * 0.26) + perp * twist + 0.5;
```

Swaps that repeatedly bought the last few slots when a shader was 1–13 over (in rough order of
payoff, extending §36):

- **Drop `sampleColor` / `vertexColor` entirely** when the draw helper always passes `Color.White` —
  check the helper, most do. Multiplying by a constant 1 costs ~4 slots for nothing.
- **Fold the quad cutoff into a falloff you already compute.** Square a `saturate((1.0 - r) * k)`
  that already reaches 0 at the quad edge and reuse it, instead of adding a separate cutoff term.
- **Compute `length(p)` once** and derive both `r` and `dir` from it.
- **Share one noise-modulation term** between two features rather than ramping churn twice.
- **Divide → multiply:** `1.0 - abs(d) / (0.09 + n * 0.10)` → `1.0 - abs(d) * (11.0 - n * 5.5)`.
- **`sqrt(x)` easing → `x * (2.0 - x)`** — same fast-then-stalling shape, several slots cheaper.
- **Scalar uv offsets** (`+ (0.5 + Time * 0.03)`) instead of `float2(...)` constructions with a
  different expression per axis.

When still over budget, cut a **decorative** term (a bloom, a pulse, an `Active` brightening the C#
already conveys through opacity/size) — never the shape math. A plainer effect reads fine; a
half-drawn silhouette reads as broken.
