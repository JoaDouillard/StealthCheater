// [EN STAND-BY] Shader de transparence circulaire
// DÉSACTIVÉ TEMPORAIREMENT - Cause des crashes Unity
// À réimplémenter plus tard quand le gameplay sera fonctionnel


// Shader "Custom/TransparentWithMask"
// {
//     Properties
//     {
//         _Color ("Color", Color) = (1,1,1,1)
//         _MainTex ("Albedo (RGB)", 2D) = "white" {}
//         _MaskCenter ("Mask Center (World)", Vector) = (0,0,0,0)
//         _MaskRadius ("Mask Radius", Float) = 2.0
//         _MinAlpha ("Min Alpha (in mask)", Range(0,1)) = 0.3
//         _FadeDistance ("Fade Distance", Float) = 0.5
//     }

//     SubShader
//     {
//         Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
//         LOD 100

//         ZWrite Off
//         Blend SrcAlpha OneMinusSrcAlpha
//         Cull Back

//         Pass
//         {
//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             #pragma target 3.0
//             #include "UnityCG.cginc"

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f
//             {
//                 float2 uv : TEXCOORD0;
//                 float4 vertex : SV_POSITION;
//                 float3 worldPos : TEXCOORD1;
//             };

//             sampler2D _MainTex;
//             float4 _MainTex_ST;
//             fixed4 _Color;
//             float3 _MaskCenter;
//             float _MaskRadius;
//             float _MinAlpha;
//             float _FadeDistance;

//             v2f vert (appdata v)
//             {
//                 v2f o;
//                 o.vertex = UnityObjectToClipPos(v.vertex);
//                 o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                 o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
//                 return o;
//             }

//             fixed4 frag (v2f i) : SV_Target
//             {
//                 // Texture de base
//                 fixed4 col = tex2D(_MainTex, i.uv) * _Color;

//                 // Calculer distance au centre du masque
//                 float distanceToCenter = distance(i.worldPos, _MaskCenter);

//                 // Alpha de base (opaque)
//                 float alpha = 1.0;

//                 // Si dans le rayon du masque, appliquer transparence
//                 if (distanceToCenter < _MaskRadius)
//                 {
//                     // Fade smooth selon la distance
//                     float fadeAmount = smoothstep(_MaskRadius - _FadeDistance, _MaskRadius, distanceToCenter);
//                     alpha = lerp(_MinAlpha, 1.0, fadeAmount);
//                 }

//                 col.a *= alpha;
//                 return col;
//             }
//             ENDCG
//         }
//     }

//     FallBack "Transparent/Diffuse"
// }
