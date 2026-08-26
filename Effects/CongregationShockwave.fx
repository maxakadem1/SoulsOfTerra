// Adapted from LunarVielmod's SuperShockwave.fx (MIT), Copyright (c) 2024 Zenovia.
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float2 epicenter;
float radius;
float strength;
float interp;

float4 CongregationShockwave(float2 coordinates : TEXCOORD0) : COLOR0
{
	float2 difference = coordinates - epicenter;
	float distanceFromCenter = length(difference * float2(uScreenResolution.x / uScreenResolution.y, 1.0));
	float bandWidth = 42.0 / uScreenResolution.y;
	float distanceToCrest = abs(distanceFromCenter - radius);
	float crest = 1.0 - smoothstep(0.0, bandWidth, distanceToCrest);
	float2 direction = difference / max(length(difference), 0.0001);
	float pulse = sin((distanceToCrest / bandWidth) * 3.14159265) * crest;
	float2 distortedCoordinates = coordinates - direction * strength * pulse * interp;

	// A restrained chromatic split makes the refractive crest visible without hiding gameplay.
	float chromaticOffset = 0.0016 * crest * interp;
	float red = tex2D(uImage0, distortedCoordinates - direction * chromaticOffset).r;
	float green = tex2D(uImage0, distortedCoordinates).g;
	float blue = tex2D(uImage0, distortedCoordinates + direction * chromaticOffset).b;
	return float4(red, green, blue, 1.0);
}

technique SpriteDrawing
{
	pass ScreenPass
	{
		PixelShader = compile ps_3_0 CongregationShockwave();
	}
};
