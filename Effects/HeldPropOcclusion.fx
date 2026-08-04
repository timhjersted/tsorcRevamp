sampler PropSampler : register(s0);
sampler MaskSampler : register(s1);

// Held-prop occlusion: hides the part of a held weapon that the wielder's own body covers, so the
// prop reads as GRIPPED rather than pasted over the sprite. Draw order alone cannot do this (the
// shaft has to pass in front of the torso but behind the fist), and a straight two-piece slice only
// approximates it, so the mask is the wielder's actual sprite alpha.
//
// PropSize        prop texture size in pixels
// GripOrigin      the prop pixel that sits at the hand (the SpriteBatch origin)
// RotCos, RotSin  cosine/sine of the prop's draw rotation
// PropScale       the prop's draw scale expressed in WIELDER FRAME PIXELS (propScale / npcScale),
//                 so the shader needs no per-pixel divide to reach the mask's coordinate space
// FlipSign        +1 or -1, mirroring the mask lookup with the NPC's sprite direction
// HandFramePixel  the hand's position within the wielder's animation frame, in frame pixels
// MaskFrameRect   float4(frameX, frameY, frameW, frameH) of the current frame, in mask pixels
// MaskTextureSize the mask sheet's size in pixels
// MaskSoftness    edge width in mask alpha units; MaskStrength scales the whole effect (0 = off)
float2 PropSize, GripOrigin, HandFramePixel, MaskTextureSize;
float RotCos, RotSin, PropScale, FlipSign, MaskSoftness, MaskStrength;
float4 MaskFrameRect;

float4 Occlude(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float4 prop = tex2D(PropSampler, c);

    // Prop pixel -> offset from the grip -> undo the draw rotation and scale -> the same offset
    // expressed in the wielder's frame, mirrored if the sprite is flipped.
    float2 d = c * PropSize - GripOrigin;
    float2 world = float2(d.x * RotCos - d.y * RotSin, d.x * RotSin + d.y * RotCos) * PropScale;
    float2 framePixel = HandFramePixel + float2(world.x * FlipSign, world.y);

    // Frame pixel -> mask atlas UV. Outside the frame there is nothing to hide behind, so the
    // lookup is clamped OUT rather than wrapped onto a neighbouring animation frame.
    float2 frameUV = framePixel / max(MaskFrameRect.zw, float2(1.0, 1.0));
    float2 atlasUV = (MaskFrameRect.xy + framePixel) / max(MaskTextureSize, float2(1.0, 1.0));
    float inside = step(0.0, frameUV.x) * step(frameUV.x, 1.0)
                 * step(0.0, frameUV.y) * step(frameUV.y, 1.0);

    float maskAlpha = tex2D(MaskSampler, atlasUV).a * inside;
    float hidden = saturate((maskAlpha - 0.5) / max(MaskSoftness, 0.01) + 0.5) * MaskStrength;

    return prop * (1.0 - hidden) * v;
}

technique HeldPropOcclusion { pass P { PixelShader = compile ps_2_0 Occlude(); } }
