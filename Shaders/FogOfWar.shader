// ============================================================
// FogOfWar.shader
// 战争迷雾着色器 - 使用透明度控制可见性
// ============================================================

Shader "RTS/FogOfWar"
{
    Properties
    {
        _MainTex ("Fog Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
        _ExploredAlpha ("Explored Alpha", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _FogColor;
            float _ExploredAlpha;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样迷雾纹理
                fixed4 fogTex = tex2D(_MainTex, i.uv);
                
                // 使用纹理的alpha值作为迷雾强度
                // alpha = 0 表示完全可见
                // alpha = 0.5 表示已探索但不可见（灰色）
                // alpha = 1 表示未探索（黑色）
                
                fixed4 result = _FogColor;
                result.a = fogTex.a;
                
                return result;
            }
            ENDCG
        }
    }
    
    // 备用着色器
    Fallback "Unlit/Transparent"
}
