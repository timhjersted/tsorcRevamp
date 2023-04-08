sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
texture noiseTexture;
sampler textureSampler = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

matrix WorldViewProjection;

float fadeOut;
float time;
float4 shaderColor;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 6.0);
    
    //Flat doubling to incrase the total intensity
    intensity *= 2;
    
    //This controls where the front of the bolt starts to curve
    float inflectionPoint = 0.82;
    //inflectionPoint *= fadeOut;
    
    //Make it fade out towards the end
    if (uv.x < inflectionPoint)
    {
        intensity = lerp(0.0, intensity, pow(uv.x / inflectionPoint, 4));
    }
    
    //Make it fade in towards the start
    if (uv.x > inflectionPoint)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - inflectionPoint), 1));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint = uv;
    
    //Zoom in on the noise texture, then shift it over time to make it appear to be flowing    
    samplePoint /= 20;
    samplePoint.x = (samplePoint.x + time * 0.0005) * 0.5;
    
    //Compress it vertically
    samplePoint.y = samplePoint.y / 2;

    //Get the noise texture at that point
    float sampleIntensity = tex2D(textureSampler, samplePoint).r;
    sampleIntensity = pow(sampleIntensity, 0.2);
    
    //Mix it with the laser color
    float4 noiseColor = float4(1.0, 1.0, 1.0, 1.0);
    noiseColor.b = pow(sampleIntensity, 5) * 1.5f * pow(intensity, 2);
    noiseColor.g = pow(sampleIntensity, 4.0) * 2.5f * pow(intensity, 2.7);
    noiseColor.r = pow(sampleIntensity, 2.0) * 0.04f * pow(intensity, 5);
    return float4(noiseColor.r, noiseColor.g, noiseColor.b, 1);

    //Mix it with 'intensity' to make it more intense near the center
    float4 effectColor = noiseColor * pow(intensity, 1) * 2;
    
    //Not the vibe i'm going for here, but looks cool as hell and will be useful later:
    //float4 effectColor = noiseColor * noiseColor * pow(intensity, 2) * 8.0;
    
    return effectColor * fadeOut * 0.2;
}

technique VortexOrb
{
    pass VortexOrbPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
}


/*
    noiseColor.b = pow(sampleIntensity, 5) * 1.5f * pow(intensity, 2.5);
    noiseColor.g = pow(sampleIntensity, 7.0) * .5f * pow(intensity, 4);
    noiseColor.r = pow(sampleIntensity, 2.0) * 0.04f * pow(intensity, 5);
*/