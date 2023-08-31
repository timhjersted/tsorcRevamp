sampler uImage0 : register(s0);
texture noiseTexture;
sampler uImage1 = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};
texture noiseTexture2;
sampler uImage2 = sampler_state
{
    Texture = (noiseTexture2);
    AddressU = wrap;
    AddressV = wrap;
};


float time; //Causes the flames to flow with time
float splitAngle; //How wide (in radians) the angle of fire is
float rotation; //Rotates the fire
float length; //The maximum length
float opacity; //Multiplies the output by this to let it fade in
float texScale;
float texScale2;
float texScale3;


//I precomputed what values I could, to save on instruction count
float rotationMinusPI;
float splitAnglePlusRotationMinusPI;
float RotationMinus2PIMinusSplitAngleMinusPI; 

//Various constants
const float PI = 3.141592;
const float TWOPI = 6.283184;
const float INVERSETWOPI = 0.1591549;
const float4 empty = float4(0, 0, 0, 0);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{    
    //Convert uv from rectangular to polar coordinates
    float2 dir = coords - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x);        
        
    //Calculate how close the current pixel is to the center line of the screen
    float dist = distance(coords, float2(0.5, 0.5));
    
    //Convert uv from rectangular to polar coordinates
    //Make three sample points, one for the voronoi noise and the other for the turbulent noise
    //They are different size textures so they must be scaled differently
    float2 samplePoint = float2(dist - time / 2, angle * INVERSETWOPI);
    float2 samplePoint2 = samplePoint;
    float2 samplePoint3 = samplePoint;
    
    samplePoint *= texScale * 2;
    samplePoint2 *= texScale2;
    samplePoint3 *= texScale3 * 2;
    
    //Calculate how intense a pixel should be based on the noise generator
    float intensity = 1;
    
    //Only draw a slice with angles between 'rotation' and 'splitAngle'. The final && is to stop it from going doing a fucky wucky when it crosses the 2Pi > 0 border.    
    if (angle > splitAnglePlusRotationMinusPI)
    {
        return empty;
    }    
    if (angle < rotationMinusPI && angle > RotationMinus2PIMinusSplitAngleMinusPI)
    {
        return empty;
    }    
    
    
    //Make it taper off near the source
    //This pushed it past the ps_2_0 instruction limit
    if (dist < 0.02)
    {
        intensity = lerp(0.0, intensity, dist / 0.02);
    }
    
    
    
    float goldIntensity = intensity;
    
    //Make it taper off on the edges    
    float edgeDistance = min(min(abs(angle - rotationMinusPI), abs(angle - rotationMinusPI + TWOPI)), min(abs(splitAnglePlusRotationMinusPI - angle), abs(angle - RotationMinus2PIMinusSplitAngleMinusPI)));
    if (abs(edgeDistance) < 0.311)
    {
        intensity = lerp(0.0, intensity, edgeDistance / 0.311);
    }
    if (abs(edgeDistance) < 0.111)
    {
        goldIntensity = lerp(0.0, goldIntensity, edgeDistance / 0.111);
    }
    
    if (dist > length)
    {
        intensity = lerp(intensity, 0, min((dist - length) * 10, 1));
        goldIntensity = pow(lerp(goldIntensity, 0, min((dist - length + 0.002) * 10, 1)), 0.35);
    }
    
    goldIntensity *= (1 - (edgeDistance - 0.211));
    goldIntensity = pow(goldIntensity, 15) * 0.5;
    
    samplePoint3.y *= 0.5;
    goldIntensity *= tex2D(uImage2, samplePoint3);
    
    
    
    
    intensity *= tex2D(uImage0, samplePoint).r;

    //Scale 'intensity' into the RGB channels. Values are fine-tuned to turn noise into a fire-like effect.
    float4 color = float4(0.2, 0.05, 0.35, 1); //Purple
    if (intensity > 0.2)
    {
        float shift = pow(1 - (intensity - 0.2), 9);
        //color.a *= shift;
        color.rgb *= shift;
    }
    
    float4 goldColor = float4(1, 0.75, 0.00, 1);
    //goldIntensity = 0;
    
    return ((color * pow(intensity, 1.25) * 2.1) + (goldColor * goldIntensity)) * opacity;
}

technique DragonBreath
{
    pass EffectPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}