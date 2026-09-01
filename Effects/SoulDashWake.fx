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

float alongStart;
float alongEnd;
float wakeIntensity;
float snapFlash;
float wakeTime;
float wakeSeed;
float wakeMode;

float Hash(float2 samplePosition)
{
	return frac(sin(dot(samplePosition, float2(127.1, 311.7)) + wakeSeed * 17.0) * 43758.5453);
}

float4 WakePixel(float2 textureCoordinates : TEXCOORD0) : COLOR0
{
	float along = lerp(alongStart, alongEnd, textureCoordinates.x);
	float acrossSigned = (textureCoordinates.y - 0.5) * 2.0;
	float flow = along * 16.0 - wakeTime * 18.0;
	float n1 = Hash(floor(float2(flow, acrossSigned * 6.0 + wakeSeed)));
	float n2 = Hash(floor(float2(flow * 2.15 + 1.7, acrossSigned * 11.0 - wakeSeed)));
	float n3 = Hash(floor(float2(flow * 0.55 - 0.4, acrossSigned * 3.2)));
	float centerSway = (n1 - n2) * 0.34 * sin(along * 3.14159265);
	float across = abs(acrossSigned - centerSway);
	float flicker = 0.78 + n1 * 0.22 + snapFlash * 0.15;

	float head = smoothstep(0.0, 0.08, along);
	float tail = 1.0 - smoothstep(0.88, 1.0, along);
	float envelope = lerp(0.55, 1.0, sin(along * 3.14159265)) * lerp(0.42, 1.0, along);
	envelope *= head * tail * flicker;
	float tongues = (n1 * 0.42 + n2 * 0.38 + n3 * 0.18) * (0.48 + along * 0.52);
	float sideBias = acrossSigned < centerSway ? lerp(0.72, 1.08, n2) : lerp(0.82, 1.16, n3);
	float flameHeight = saturate((envelope * 0.48 + tongues * 0.68) * sideBias);
	float fractureNoise = Hash(float2(floor(along * 19.0), wakeSeed + floor(across * 3.0)));
	float fracture = step(0.3 + across * 0.12, fractureNoise);

	float3 deepTeal = float3(0.01, 0.08, 0.09);
	float3 soulFire = float3(0.2, 1.0, 0.72);
	float3 paleSoul = float3(0.9, 1.0, 0.97);
	float intensity = saturate(wakeIntensity);

	if (wakeMode > 1.5)
	{
		float core = pow(saturate(1.0 - across / max(flameHeight * 0.42, 0.08)), 3.2);
		float embers = step(0.74, n2) * core;
		float3 color = lerp(soulFire, paleSoul, saturate(core + snapFlash));
		float alpha = saturate(core * 1.15 + embers * 0.55) * intensity * fracture;
		return float4(color, alpha);
	}

	if (wakeMode > 0.5)
	{
		float bloom = pow(saturate(1.0 - across / max(flameHeight * 1.35, 0.2)), 1.35);
		float3 color = lerp(deepTeal, soulFire, saturate(bloom * 0.85 + n3 * 0.15));
		float alpha = bloom * (0.16 + n1 * 0.08) * intensity * lerp(0.22, 1.0, fracture);
		return float4(color, alpha);
	}

	float body = smoothstep(flameHeight, flameHeight * 0.18, across);
	float core = pow(saturate(1.0 - across / max(flameHeight * 0.5, 0.1)), 2.4);
	float ember = step(0.62, n2) * smoothstep(flameHeight, flameHeight * 0.12, across);
	float3 color = lerp(deepTeal, soulFire, saturate(body));
	color = lerp(color, paleSoul, saturate(core * 0.9 + ember * 0.65 + snapFlash * 0.35));
	float alpha = saturate(body * 0.82 + core * 0.55 + ember * 0.4) * intensity * fracture;
	return float4(color, alpha);
}

technique SoulDashWake
{
	pass WakePass
	{
		PixelShader = compile ps_3_0 WakePixel();
	}
};
