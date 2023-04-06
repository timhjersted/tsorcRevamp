sampler noiseTexture : register(s0);
float textureSize;
float2 effectSize;
float4 effectColor1;
float4 effectColor2;
float time;
float ringProgress;
float fadePercent;
float scaleFactor;
float colorSplitAngle;
float deathProgress;


const float PI = 3.141592;
const float TWOPI = 6.283185;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    //Normalize pixel coordinates (from 0 to 1) and compensate for effect size distortion
    float2 uv = coords * textureSize;
    uv.x /= effectSize.x;
    uv.y /= effectSize.y;
    
    //Calculate how close the current pixel is to the center line of the screen
    float distanceIntensity = distance(uv, float2(0.5, 0.5)) * 2;
    
    // * (1f/50f)
    //Calculate how close the current pixel is from a ring of radius 'ringProgress'
    float ringDistance = 1 - abs(ringProgress - distanceIntensity);
    
    ringDistance = pow(ringDistance, 11);
    
    
    //Convert uv from rectangular to polar coordinates
    float2 dir = uv - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x);
    angle += PI;
    
    if (angle > colorSplitAngle)
    {
        return float4(0, 0, 0, 0);
    }
    
    //Blend slightly between colors
    float lerpPercent = angle / colorSplitAngle;
    float4 auraColor = lerp(effectColor1, effectColor2, pow(lerpPercent, 1));
    //return lerpPercent;
    
    
    float2 samplePoint = float2(distanceIntensity, angle / TWOPI);
    
    //Stretch it so it looks good
    samplePoint = samplePoint * scaleFactor / effectSize;
    samplePoint.y *= 3;
    
    //Offset it based on time to create the flowing effect
    samplePoint.x -= time;
    
    return tex2D(noiseTexture, samplePoint).r * auraColor * ringDistance / distanceIntensity;
}


technique AttraidiesAura
{
    pass AttraidiesAuraPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}


/*
//Could not fit this in complexity limit
ringExponent = 16;
//Check whether the pixel is inside or outside the ring
if (distanceIntensity > ringProgress)
{
    ringExponent = 6;
}*/