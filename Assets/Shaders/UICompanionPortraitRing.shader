Shader "Custom/UI/CompanionPortraitRing"
{
    Properties
    {
        [PerRendererData] _MainTex("Avatar", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _HpFill("HP Fill", Range(0,1)) = 1
        _HpColor("HP Ring", Color) = (0.25,0.9,0.38,1)
        _RingBgColor("Ring BG", Color) = (1,1,1,0.2)
        _InnerBgColor("Inner BG", Color) = (0,0,0,0.65)
        _InnerRadius("Inner Radius", Range(0,1)) = 0.66
        _RingWidth("Ring Width", Range(0.01,0.5)) = 0.13
        _AvatarScale("Avatar Scale", Range(0.1,1)) = 0.79
        _AntiAlias("Anti Alias", Range(0.001,0.05)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _HpColor;
            fixed4 _RingBgColor;
            fixed4 _InnerBgColor;
            float _HpFill;
            float _InnerRadius;
            float _RingWidth;
            float _AvatarScale;
            float _AntiAlias;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 centered = IN.uv - 0.5;
                float dist = length(centered);
                float outerRadius = 0.5;
                float ringInner = outerRadius - _RingWidth;

                float outerMask = 1.0 - smoothstep(outerRadius - _AntiAlias, outerRadius + _AntiAlias, dist);
                float innerMask = 1.0 - smoothstep(ringInner - _AntiAlias, ringInner + _AntiAlias, dist);
                float ringMask = saturate(outerMask - innerMask);

                float2 dir = dist > 1e-5 ? centered / dist : float2(0, 1);
                float angle = atan2(dir.x, dir.y);
                float normalizedAngle = frac((angle + UNITY_PI) / (2.0 * UNITY_PI));
                float hpArc = step(normalizedAngle, saturate(_HpFill));

                float avatarRadius = _InnerRadius * 0.5;
                float avatarMask = 1.0 - smoothstep(avatarRadius - _AntiAlias, avatarRadius + _AntiAlias, dist);

                float2 avatarUv = (IN.uv - 0.5) / max(_AvatarScale, 0.001) + 0.5;
                fixed4 avatarTex = tex2D(_MainTex, avatarUv);
                fixed4 avatarColor = lerp(_InnerBgColor, avatarTex * IN.color, saturate(avatarTex.a));
                fixed4 ringColor = lerp(_RingBgColor, _HpColor, hpArc);

                fixed4 finalColor = ringColor * ringMask + avatarColor * avatarMask;
                finalColor.a = saturate(ringMask * ringColor.a + avatarMask * avatarColor.a);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
