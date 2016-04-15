Shader "Custom/TriplanarAddPass"
{
	Properties
	{
		//Control Texture  ("Splat Map")
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}

		//Terrain textures - each weighted according to the corresponding color channel in the control texture
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}

		// Normal Maps
		[HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}

		[HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 1.0

		//_DiffuseMap("Diffuse Map", 2D) = "white" {}
		_TextureScale("Texture Scale", float) = 1
		_TriplanarBlendSharpness("Blend Sharpness", float) = 1
		_Color("Main Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags
		{
			"SplatCount" = "4"
			"Queue" = "Geometry-99"
			"RenderType" = "Opaque"
			"IgnoreProjector" = "True"
		}
		LOD 200

		//TERRAIN PASS
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Lambert decal:add


		uniform sampler2D _Control;
		uniform sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
		uniform sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
		float _TextureScale;
		float _TriplanarBlendSharpness;
		uniform fixed4 _Color;

		half _Smoothness0;
		half _Smoothness1;
		half _Smoothness2;
		half _Smoothness3;


		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float2 uv_Control : TEXCOORD0;
			float2 uv_Splat0 : TEXCOORD1;
			float2 uv_Splat1 : TEXCOORD2;
			float2 uv_Splat2 : TEXCOORD3;
			float2 uv_Splat3 : TEXCOORD4;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 splat_control = tex2D(_Control, IN.uv_Control);
			half weight = dot(splat_control, half4(1, 1, 1, 1));
			fixed4 mixedDiffuse = 0.0f;
			half defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
			//fixed3 norms;

			// Normalize weights before lighting and restore weights in final modifier functions so that the overal
			// lighting result can be correctly weighted.
			//splat_control /= (weight + 1e-3f);	

			mixedDiffuse += splat_control.r * (tex2D(_Splat0, IN.uv_Splat0) * .5 + tex2D(_Splat0, IN.uv_Splat0 * .75) * .25 + tex2D(_Splat0, IN.uv_Splat0 * .5) * .25);
			mixedDiffuse += splat_control.g * (tex2D(_Splat1, IN.uv_Splat1) * .5 + tex2D(_Splat1, IN.uv_Splat1 * .75) * .25 + tex2D(_Splat0, IN.uv_Splat1 * .5) * .25);
			mixedDiffuse += splat_control.b * (tex2D(_Splat2, IN.uv_Splat2) * .5 + tex2D(_Splat2, IN.uv_Splat2 * .75) * .25 + tex2D(_Splat0, IN.uv_Splat2 * .5) * .25);
			mixedDiffuse += splat_control.a * (tex2D(_Splat3, IN.uv_Splat3) * .5 + tex2D(_Splat3, IN.uv_Splat3 * .75) * .25 + tex2D(_Splat0, IN.uv_Splat3 * .5) * .25);

			fixed4 nrm = 0.0f;
			nrm += splat_control.r * (tex2D(_Normal0, IN.uv_Splat0) * .5 + tex2D(_Normal0, IN.uv_Splat0 * .75) * .25 + tex2D(_Normal0, IN.uv_Splat0 * .5) * .25);
			nrm += splat_control.g * (tex2D(_Normal1, IN.uv_Splat1) * .5 + tex2D(_Normal1, IN.uv_Splat1 * .75) * .25 + tex2D(_Normal0, IN.uv_Splat1 * .5) * .25);
			nrm += splat_control.b * (tex2D(_Normal2, IN.uv_Splat2) * .5 + tex2D(_Normal2, IN.uv_Splat2 * .75) * .25 + tex2D(_Normal0, IN.uv_Splat2 * .5) * .25);
			nrm += splat_control.a * (tex2D(_Normal3, IN.uv_Splat3) * .5 + tex2D(_Normal3, IN.uv_Splat3 * .75) * .25 + tex2D(_Normal0, IN.uv_Splat3 * .5) * .25);
			//mixedNormal = UnpackNormal(nrm);
			o.Normal = UnpackNormal(nrm);

			////Find our UVs for each axis based on world position of the fragment.
			//half2 yUV = IN.worldPos.xz / _TextureScale;
			//half2 xUV = IN.worldPos.zy / _TextureScale;
			//half2 zUV = IN.worldPos.xy / _TextureScale;

			////Now do texture samples from our diffuse map with each of the 3 UV set's we've just made.
			//half3 yDiffSplat0 = tex2D(_Splat0, yUV);
			//half3 xDiffSplat0 = tex2D(_Splat0, xUV);
			//half3 zDiffSplat0 = tex2D(_Splat0, zUV);

			//half3 yDiffSplat1 = tex2D(_Splat1, yUV);
			//half3 xDiffSplat1 = tex2D(_Splat1, xUV);
			//half3 zDiffSplat1 = tex2D(_Splat1, zUV);

			//half3 yDiffSplat2 = tex2D(_Splat2, yUV);
			//half3 xDiffSplat2 = tex2D(_Splat2, xUV);
			//half3 zDiffSplat2 = tex2D(_Splat2, zUV);

			//half3 yDiffSplat3 = tex2D(_Splat3, yUV);
			//half3 xDiffSplat3 = tex2D(_Splat3, xUV);
			//half3 zDiffSplat3 = tex2D(_Splat3, zUV);

			////Normals
			//half3 yNormSplat0 = tex2D(_Normal0, yUV);
			//half3 xNormSplat0 = UnpackNormal(tex2D(_Normal0, xUV));
			//half3 zNormSplat0 = UnpackNormal(tex2D(_Normal0, zUV));

			//half3 yNormSplat1 = UnpackNormal(tex2D(_Normal1, yUV));
			//half3 xNormSplat1 = UnpackNormal(tex2D(_Normal1, xUV));
			//half3 zNormSplat1 = UnpackNormal(tex2D(_Normal1, zUV));

			//half3 yNormSplat2 = UnpackNormal(tex2D(_Normal2, yUV));
			//half3 xNormSplat2 = UnpackNormal(tex2D(_Normal2, xUV));
			//half3 zNormSplat2 = UnpackNormal(tex2D(_Normal2, zUV));

			//half3 yNormSplat3 = UnpackNormal(tex2D(_Normal3, yUV));
			//half3 xNormSplat3 = UnpackNormal(tex2D(_Normal3, xUV));
			//half3 zNormSplat3 = UnpackNormal(tex2D(_Normal3, zUV));

			////Get the absolute value of the world normal.
			////Put the blend weights to the power of the BlendSharpness, the higher the value,
			////the sharper the transition between the planar maps will be.
			//half3 blendWeights = pow(abs(IN.worldNormal), _TriplanarBlendSharpness);

			////Divide our blend mask by the sum of it's components, this will make x+y+z=1
			//blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

			////Finally, blend together all three samples based on the blend mask.
			//mixedDiffuse = splat_control.r * (xDiffSplat0 * blendWeights.x + yDiffSplat0 * blendWeights.y + zDiffSplat0 * blendWeights.z);
			//mixedDiffuse += splat_control.g * (xDiffSplat1 * blendWeights.x + yDiffSplat1 * blendWeights.y + zDiffSplat1 * blendWeights.z);
			//mixedDiffuse += splat_control.b * (xDiffSplat2 * blendWeights.x + yDiffSplat2 * blendWeights.y + zDiffSplat2 * blendWeights.z);
			//mixedDiffuse += splat_control.a * (xDiffSplat3 * blendWeights.x + yDiffSplat3 * blendWeights.y + zDiffSplat3 * blendWeights.z);
			o.Albedo = mixedDiffuse.rgb *_Color;
			//o.Alpha = weight;

			//norms = splat_control.r *(xNormSplat0 * blendWeights.x + yNormSplat0 * blendWeights.y + zNormSplat0 * blendWeights.z);
			//norms += splat_control.r *(xNormSplat1 * blendWeights.x + yNormSplat1 * blendWeights.y + zNormSplat1 * blendWeights.z);
			//norms += splat_control.r *(xNormSplat2 * blendWeights.x + yNormSplat2 * blendWeights.y + zNormSplat2 * blendWeights.z);
			//norms += splat_control.r *(xNormSplat3 * blendWeights.x + yNormSplat3 * blendWeights.y + zNormSplat3 * blendWeights.z);
			//o.Normal = norms;
		}
		ENDCG
	}
	//Specify dependency shaders
	//Dependency "AddPassShader" = "Custom/TriplanarAddPass"
	FallBack "Diffuse"
}
