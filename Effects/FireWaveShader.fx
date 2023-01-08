sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
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

/* Was gonna dynamically generate the noise but ps_2_0's instruction count limit said "Fuck No"
* The turbulent noise I generated in paint.net ended up looking better anyway, leaving this in just because it'll be useful
float2 hash(float2 p) {

    //p = ;
    return -1.0 + 2.0 * frac(sin(float2(dot(p, float2(127.1, 311.7)),
        dot(p, float2(269.5, 183.3)))) * 43758.5453123);
}

float noise(in float2 p) {

    /*
    const float K1 = 0.366025404f; // (sqrt(3)-1)/2;
    const float K2 = 0.211324865f; // (3-sqrt(3))/6;
    float2 i = float2(0,0);// = floor(p + (p.x + p.y) * K1);
    //return i.x; //Works
    i.x = floor(p.x + (p.x + p.y) * K1);
    //i.y = floor(p.y + (p.x + p.y) * K1);
    //return 1; //works
    //return K1; //works
    //return p.x //Compilation fails
    //return i.x //Compilation fails

    float2 a = p - i + (i.x + i.y) * K2;
    float2 o = step(a.yx, a.xy);
    float2 b = a - o + K2;
    float2 c = a - 1.0 + 2.0 * K2;

    float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    h = pow(h, 4);
    float3 n = h * float3(dot(a, hash(i)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));


    float prod = dot(n, float3(70.0, 70.0, 70.0));
    
    return p;
}

float fbm(float2 p) {

    float intensity = 1;
    float2x2 m = float2x2(1.6, 1.2, -1.2, 1.6);
    //for (float z = 1; z < 5; z++) {        

    const float K1 = 0.366025404f; // (sqrt(3)-1)/2;
    const float K2 = 0.211324865f; // (3-sqrt(3))/6;
    float2 i = floor(p + (p.x + p.y) * K1);

    float2 a = p - i + (i.x + i.y) * K2;
    float2 o = float2(1, 1); //step(a.yx, a.xy);
    float2 b = a - o + K2;
    float2 c = a - 1.0 + 2.0 * K2;

    float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    h = h * h; //Works
    float3 r = h * h; //Doing it a second time fails to compile

    float2 y = float2(1, 1);


    float2 nx = -1.0 + 2.0 * frac(sin(float2(dot(i, float2(127.1, 311.7)),
        dot(i, float2(269.5, 183.3)))) * 43758.5453123);

    float2 ny = -1.0 + 2.0 * frac(sin(float2(dot(i + o, float2(127.1, 311.7)),
        dot(i + o, float2(269.5, 183.3)))) * 43758.5453123);

    float2 nz = -1.0 + 2.0 * frac(sin(float2(dot(i + y, float2(127.1, 311.7)),
        dot(i + y, float2(269.5, 183.3)))) * 43758.5453123);


    //float3 n = float3(dot(a, nx), dot(b, ny), dot(c, nz));
    //return n.x;

    //float ahh = dot(n, float3(70.0, 70.0, 70.0));
    //float aaa = h.x;
    float ahh = (dot(a, nx) * 70 * h.x) + (dot(b, ny) * 70 * h.y) + (dot(c, nz) * 70);



    intensity = intensity + ahh;

    return intensity;
    p = mul(p, m);
    //}

    intensity = 0.7 + 0.3 * intensity;
    return intensity;
}*/

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{


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
    float dist = distance(pixelatedCoord, fragCenter) * 2;


    //Make the fire more intense close to the edge of the radius, tapering off with distance
    float distanceFactor = (1.0f - (modTime - dist));


    //return float4(dist * dist, dist * dist, dist * dist, 1);

    if (modTime > dist) {
        //Make its intensity taper off slow when inside the radius
        distanceFactor = pow(abs(distanceFactor), 45.0);

    }
    else
    {
        //And taper off faster when outside it
        distanceFactor = 1.0 + (modTime - dist);
        distanceFactor = pow(abs(distanceFactor), 290.0);

        //distanceFactor = 1;
    }


    //float2 uv = coords.xy / uResolution;

    //Calculate how intense a pixel should be based on the noise generator
    float intensity = tex2D(uImage0, pixelatedCoord * 1).r * sampleColor;
    intensity = pow(intensity, 0.2);
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
}

technique FireWaveShader
{
    pass FireWaveShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}