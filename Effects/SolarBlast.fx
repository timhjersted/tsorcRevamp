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
    noiseColor.r = pow(sampleIntensity, 5) * 1.5f * pow(intensity, 2.5);
    noiseColor.g = pow(sampleIntensity, 7.0) * .5f * pow(intensity, 4);
    noiseColor.b = pow(sampleIntensity, 2.0) * 0.04f * pow(intensity, 5);
    return float4(noiseColor.r, noiseColor.g, noiseColor.b, 1);

    //Mix it with 'intensity' to make it more intense near the center
    float4 effectColor = noiseColor * pow(intensity, 1) * 2;
    
    //Not the vibe i'm going for here, but looks cool as hell and will be useful later:
    //float4 effectColor = noiseColor * noiseColor * pow(intensity, 2) * 8.0;
    
    return effectColor * fadeOut;
}

technique SolarBlast
{
    pass SolarBlastPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
}

/*
//float uTime = 0.5;
    float pixelSize = 0.001;
    //Calculate the radius based on the time
    //Ingame the radius will be controlled by an external variable instead
    float modTime = frac(uTime / 10) * 1.0;
    modTime = uSaturation;
    //float modTime = uTime;
    //float radius = modTime;
    //float2 fragCenter = uImageSize0.xy / 2.0;
    float2 fragCenter = float2(0.5, 0.5);

    //Pixelation effect, achieved via fucking with the coordinates. Commented out right now because it nukes the detail on the noise map from orbit.
    float2 pixelatedCoord = coords;
    //pixelatedCoord.x = coords.x - fmod(coords.x, pixelSize);
    //pixelatedCoord.y = coords.y - fmod(coords.y, pixelSize);

    //Calculate how far the current pixel is from the center
    float dist = coords.x;


    //Make the fire more intense close to the edge of the radius, tapering off with distance
    float distanceFactor = (1.0f - (modTime - dist));
    //return float4(dist * dist, dist * dist, dist * dist, 1);

    if (coords.x > 0.9)
    {
        distanceFactor = pow(abs(0.9 - coords.x), 9.0);

    }
    else
    {
        distanceFactor = pow(abs(coords.x), 4.0);
    }
    return float4(distanceFactor, distanceFactor, distanceFactor, distanceFactor);


    //float2 uv = coords.xy / uResolution;

    //Calculate how intense a pixel should be based on the noise generator
    float intensity = tex2D(textureSampler, pixelatedCoord * 0.125).r * sampleColor;
    intensity = pow(intensity, 0.2);
    //return float4(intensity, intensity, intensity, intensity);
    //intensity -= 0.3;
    //intensity /= 0.7;
    //return float4(intensity, intensity, intensity, 1);

    //float intensity = 1;
    //Calculate and output the final color of the pixel    
    distanceFactor = distanceFactor * 2.5f;
    //return float4(distanceFactor, intensity, 1, 0.5);

    if (modTime > 1.5) {
        distanceFactor /= 5 * (modTime - 0.5);
    }

    float r = pow(intensity, 5) * 1.5f * pow(distanceFactor, 1.5);
    float g = pow(intensity, 7.0) * .5f * pow(distanceFactor, 3);
    float b = pow(intensity, 2.0) * 0.04f * pow(distanceFactor, 4);
    return float4(r , g , b , 1) * sampleColor;
*/