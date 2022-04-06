/*
 * AuthorNote:
 * Created By: WangYu  Date: 2022-03-25
*/

Shader "Hidden/rrtt_2323/BlitProcessing"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"

    #pragma shader_feature _BIAS_GAMMA_SPACE_COLOR_ON

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };
    struct v2f
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
    
    v2f vert_default (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        return o;
    }
    ENDCG
    
    SubShader
    {
        Cull Off Fog { Mode Off }
        ZWrite Off ZTest Always
        
        Pass
        {
            Blend One Zero
            
            Name "0"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = float2(o.uv.x, 1.0 - o.uv.y); // 反转uv
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                col.rgb = GammaToLinearSpace(col.rgb);
                // unity不管透明的纹理，我管！
                if(col.a > 0 && col.a < 1)
                {
                    col.a = GammaToLinearSpace(col.a);
                }
                
                return col;
            }
            ENDCG
        }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            
            Name "1"
            CGPROGRAM
            #pragma vertex vert_default
            #pragma fragment frag
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // unity 的那个 sRGB 功能并不会对透明纹理进行处理，所以需要我们进行特殊处理
                // 人为的往 gamma 空间转一次，也就是 col^1.0/2.2 以抵消混合时，unity 做的那次 col^2.2
                if(col.a > 0 && col.a < 1)
                {
                    fixed3 baseCol = col.rgb;
                    // 除以这个值，颜色就会变暗一点，就是 gamma 空间下的颜色
                    // 不除这个值，就会亮一点，就是 unity sRGB 处理后的值
                    #ifdef _BIAS_GAMMA_SPACE_COLOR_ON
                    {
                        baseCol /= unity_ColorSpaceDouble;
                    }
                    #endif
                    
                    fixed3 l2gCol = LinearToGammaSpace(baseCol);
                    col.rgb = l2gCol;
                    
                    col.a = LinearToGammaSpace(col.a);
                }
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack off
}
