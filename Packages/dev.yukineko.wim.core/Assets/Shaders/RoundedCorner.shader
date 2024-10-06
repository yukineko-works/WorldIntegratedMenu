Shader "YNWorks/UI/RoundedCorners"
{
    Properties {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _r ("r", Vector) = (0,0,0,0)
        _halfSize ("halfSize", Vector) = (0,0,0,0)
        _rect2props ("rect2props", Vector) = (0,0,0,0)
        _OuterUV ("image outer uv", Vector) = (0, 0, 1, 1)
    }

    SubShader {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            float4 _r;
            float4 _halfSize;
            float4 _rect2props;
            float4 _OuterUV;
            sampler2D _MainTex;
            float4 _ClipRect;
            fixed4 _TextureSampleAdd;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            inline float2 translate(float2 samplePosition, float2 offset){
                return samplePosition - offset;
            }

            float2 rotate(float2 samplePosition, float rotation){
                const float PI = 3.14159;
                float angle = rotation * PI * 2 * -1;
                float sine, cosine;
                sincos(angle, sine, cosine);
                return float2(cosine * samplePosition.x + sine * samplePosition.y, cosine * samplePosition.y - sine * samplePosition.x);
            }

            float circle(float2 position, float radius){
                return length(position) - radius;
            }

            inline fixed4 mixAlpha(fixed4 mainTexColor, fixed4 color, float sdfAlpha){
                fixed4 col = mainTexColor * color;
                col.a = min(col.a, sdfAlpha);
                return col;
            }

            float rectangle(float2 samplePosition, float2 halfSize){
                float2 distanceToEdge = abs(samplePosition) - halfSize;
                float outsideDistance = length(max(distanceToEdge, 0));
                float insideDistance = min(max(distanceToEdge.x, distanceToEdge.y), 0);
                return outsideDistance + insideDistance;
            }

            float AntialiasedCutoff(float distance){
                float distanceChange = fwidth(distance) * 0.5;
                return smoothstep(distanceChange, -distanceChange, distance);
            }

            float CalcAlphaForIndependentCorners(float2 samplePosition, float2 halfSize, float4 rect2props, float4 r){
                samplePosition = (samplePosition - .5) * halfSize * 2;

                float r1 = rectangle(samplePosition, halfSize);

                float2 r2Position = rotate(translate(samplePosition, rect2props.xy), .125);
                float r2 = rectangle(r2Position, rect2props.zw);

                float2 circle0Position = translate(samplePosition, float2(-halfSize.x + r.x, halfSize.y - r.x));
                float c0 = circle(circle0Position, r.x);

                float2 circle1Position = translate(samplePosition, float2(halfSize.x - r.y, halfSize.y - r.y));
                float c1 = circle(circle1Position, r.y);

                float2 circle2Position = translate(samplePosition, float2(halfSize.x - r.z, -halfSize.y + r.z));
                float c2 = circle(circle2Position, r.z);

                float2 circle3Position = translate(samplePosition, -halfSize + r.w);
                float c3 = circle(circle3Position, r.w);

                float dist = max(r1,min(min(min(min(r2, c0), c1), c2), c3));
                return AntialiasedCutoff(dist);
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uvSample = i.uv;
                uvSample.x = (uvSample.x - _OuterUV.x) / (_OuterUV.z - _OuterUV.x);
                uvSample.y = (uvSample.y - _OuterUV.y) / (_OuterUV.w - _OuterUV.y);

                half4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                if (color.a <= 0) {
                    return color;
                }

                float alpha = CalcAlphaForIndependentCorners(uvSample, _halfSize.xy, _rect2props, _r);

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return mixAlpha(tex2D(_MainTex, i.uv), i.color, alpha);
            }

            ENDCG
        }
    }
}