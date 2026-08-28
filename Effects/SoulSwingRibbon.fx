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
float2 uImageSize2;
float4 uShaderSpecificData;

float4x4 uTransform;
float ribbonTime;
float ribbonIntensity;

struct VSInput
{
	float4 position : POSITION0;
	float4 color : COLOR0;
	float2 texCoord : TEXCOORD0;
};

struct VSOutput
{
	float4 position : POSITION0;
	float4 color : COLOR0;
	float2 texCoord : TEXCOORD0;
};

VSOutput RibbonVertex(VSInput input)
{
	VSOutput output;
	output.position = mul(float4(input.position.xy, 0.0, 1.0), uTransform);
	output.color = input.color;
	output.texCoord = input.texCoord;
	return output;
}

float4 RibbonPixel(VSOutput input) : COLOR0
{
	float along = input.texCoord.x;
	float across = abs(input.texCoord.y - 0.5) * 2.0;
	float startFade = smoothstep(0.0, 0.08, along);
	float endFade = 1.0 - smoothstep(0.82, 1.0, along);
	float pulse = 0.92 + 0.08 * sin(along * 18.0 - ribbonTime * 5.5);

	// Hollow core with a brighter rim, matching the soul-orb visual language.
	float body = 1.0 - smoothstep(0.38, 1.0, across);
	float rim = pow(saturate(1.0 - abs(across - 0.64) * 4.2), 1.85);
	float coreHollow = saturate(across * 1.15);
	float alpha = saturate(body * coreHollow * 0.22 + rim * 0.92) * startFade * endFade * pulse;
	alpha *= saturate(uOpacity) * saturate(ribbonIntensity) * input.color.a;

	float3 rimLight = saturate(uColor * 1.35 + float3(0.18, 0.22, 0.2));
	float3 color = lerp(uColor * 0.42, rimLight, saturate(rim * 1.15 + across * 0.2));
	return float4(color, alpha);
}

technique SoulSwingRibbon
{
	pass RibbonPass
	{
		VertexShader = compile vs_3_0 RibbonVertex();
		PixelShader = compile ps_3_0 RibbonPixel();
	}
};
