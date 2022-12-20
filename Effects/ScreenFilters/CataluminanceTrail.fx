matrix WorldViewProjection;
texture noiseTexture;
sampler textureSampler = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float fadeOut;
float time;
float4 shaderColor;
float length;

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
    //float pixelSize = 0.0005;
    //uv.x = uv.x - fmod(uv.x, pixelSize);
    //uv.y = uv.y - fmod(uv.y, pixelSize  * 50);
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 4.0);
    
    //Flat doubling to incrase the total intensity
    //intensity *= 2;
    
    //This controls where the front of the bolt starts to curve
    float inflectionPoint = 0.82;
    //inflectionPoint *= fadeOut;
    
    //Make it fade out towards the end
    if (uv.x < inflectionPoint)
    {
        //intensity = lerp(0.0, intensity, pow(uv.x / inflectionPoint, 1));
    }
    
    float start = 0.99;
    float end = 0.1;
    float yStart = 0.7;
    //Make it fade in towards the start
    if (uv.x > start)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - start), 3));
    }
    if (uv.x < end)
    {
        intensity = lerp(0.0, intensity, pow((uv.x) / (end), 3));
    }
    float yEnd = 1 - yStart;
    if (uv.y > yStart)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.y) / (1 - yStart), 3));
    }
    if (uv.y < yEnd)
    {
        intensity = lerp(0.0, intensity, pow((uv.y) / (yEnd), 3));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint1 = uv * 3;
    
    //Zoom in on the noise texture, then shift it over time to make it appear to be flowing    
    //samplePoint /= 70;
    samplePoint1.x = (samplePoint1.x) * length / 10000.0; // ;
    
    //Compress it vertically
    samplePoint1.y = (samplePoint1.y / 10);
    
    float2 samplePoint2 = samplePoint1;
    samplePoint1.y -= (time * 0.1);
    samplePoint1.x -= (time * 0.1);
    samplePoint2.y += (time * 0.1);
    samplePoint2.x += (time * 0.101);

    //Get the noise texture at that point
    float sampleIntensity = tex2D(textureSampler, samplePoint1).r;
    sampleIntensity *= tex2D(textureSampler, samplePoint2).r;
    
    //Mix it with the laser color
    float4 noiseColor = 1 - float4(1.0, 1.0, 1.0, 1.0);
    noiseColor.r = sampleIntensity * shaderColor.r;
    noiseColor.b = sampleIntensity * shaderColor.b;
    noiseColor.g = sampleIntensity * shaderColor.g;
    
    //return noiseColor;
    //
    //Mix it with 'intensity' to make it more intense near the center
    //float4 effectColor = pow(noiseColor, 0.85) * pow(intensity, 2) * 1.0;
    
    //Not the vibe i'm going for here, but looks cool as hell and will be useful later:
    if (intensity > 0.5)
    {
        intensity = 0.5;
    }
    
    float4 effectColor = pow(noiseColor, 1) * 13.0 * pow(intensity, 1) * pow(shaderColor, 2.5);
    return effectColor;
    effectColor = float4(0, 0, 0, 0);
    sampleIntensity = sampleIntensity * sampleIntensity;
    if (sampleIntensity * intensity > 0.15)
    {
        effectColor = float4(1, 1, 1, 1);
    }
    else if (sampleIntensity * intensity > 0.08)
    {
        effectColor = float4(1, 0.8, 0.95, 1);
    }
    else if (sampleIntensity * intensity > 0.03)
    {
        effectColor = float4(1, 0.4, 0.8, 0.5);
    }
    else if (sampleIntensity * intensity > 0.01)
    {
        effectColor = float4(1, 0.4, 0.8, 0.25);
    }
    else if (sampleIntensity * intensity > 0.001)
    {
        effectColor = float4(1, 0.4, 0.8, 0.125);
    }
    
    
    return effectColor / pow(fadeOut, 1);
}


technique CataluminanceTrail
{
    pass CataluminanceTrailPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};