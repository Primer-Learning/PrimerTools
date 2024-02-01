Shader "Primer/StackedAreas" {
    Properties{
        _VisibleMin("VisibleMin", Vector) = (0,0,0,0)
        _VisibleMax("VisibleMax", Vector) = (1,1,1,0)
        _Min("Min", Vector) = (0,0,0,0)
        _Max("Max", Vector) = (1,1,1,0)
        _Color1("Color1", Color) = (0,0,0,1)
        _Color2("Color2", Color) = (0,0,0,1)
        _Color3("Color3", Color) = (0,0,0,1)
        _Color4("Color4", Color) = (0,0,0,1)
        _Color5("Color5", Color) = (0,0,0,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
    }

    SubShader{
        Cull off
        Tags { "RenderType" = "Opaque" }
        //Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

        CGPROGRAM
        //Notice the "vertex:vert" at the end of the next line
        #pragma surface surf Standard fullforwardshadows vertex:vert
        //#pragma surface surf Standard fullforwardshadows vertex:vert alpha:premul

        sampler2D _MainTex;

        struct Input {
            float2 uv_MainTex;
            float3 coord;
        };

        int _ValuesCount = 900;
        float _ClippingNumber = 900; //When filling a range over time, use this to clip off the descent to filler zeroes
        float _FuncValues1[900];
        float _FuncValues2[900];
        float _FuncValues3[900];
        float _FuncValues4[900];

        fixed4 _Color1;
        fixed4 _Color2;
        fixed4 _Color3;
        fixed4 _Color4;
        fixed4 _Color5;
        float4 _Min;
        float4 _Max;
        float4 _VisibleMin;
        float4 _VisibleMax;

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.coord.x = v.vertex.x;
            o.coord.y = v.vertex.y;
            o.coord.z = v.vertex.z;
        }

        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed4 c1 = _Color1;
            fixed4 c2 = _Color2;
            fixed4 c3 = _Color3;
            fixed4 c4 = _Color4;
            fixed4 c5 = _Color5;

            int numSteps = _ValuesCount - 1;
            float step = (_Max.x - _Min.x) / numSteps;
            int lowerPointIndex = floor((IN.coord.x - _Min.x) / step);
            float yThreshold1 = lerp(_FuncValues1[lowerPointIndex], _FuncValues1[lowerPointIndex + 1], (IN.coord.x - lowerPointIndex * step) / step);
            ///*
            float yThreshold2 = lerp(_FuncValues1[lowerPointIndex] + _FuncValues2[lowerPointIndex],
                                     _FuncValues1[lowerPointIndex + 1] + _FuncValues2[lowerPointIndex + 1],
                                     (IN.coord.x - lowerPointIndex * step) / step);
            float yThreshold3 = lerp(_FuncValues1[lowerPointIndex] + _FuncValues2[lowerPointIndex] + _FuncValues3[lowerPointIndex],
                                     _FuncValues1[lowerPointIndex + 1] + _FuncValues2[lowerPointIndex + 1] + _FuncValues3[lowerPointIndex + 1],
                                     (IN.coord.x - lowerPointIndex * step) / step);
            float yThreshold4 = lerp(_FuncValues1[lowerPointIndex] + _FuncValues2[lowerPointIndex] + _FuncValues3[lowerPointIndex] + _FuncValues4[lowerPointIndex],
                                     _FuncValues1[lowerPointIndex + 1] + _FuncValues2[lowerPointIndex + 1] + _FuncValues3[lowerPointIndex + 1] + _FuncValues4[lowerPointIndex + 1],
                                     (IN.coord.x - lowerPointIndex * step) / step);
            //float yThreshold5 = yThreshold1 + 0.1f;
            //*/

            fixed4 c;
            if (IN.coord.y < yThreshold1) { c = c1; }
            else if (IN.coord.y < yThreshold2) { c = c2; }
            else if (IN.coord.y < yThreshold3) { c = c3; }
            else if (IN.coord.y < yThreshold4) { c = c4; }
            //else if (IN.coord.y < yThreshold5) { c = c5; }
            else { c = c5; }
            o.Albedo = (0,0,0);
            //o.Albedo = c;
            //o.Alpha = c.a;
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Emission = c.rgb;
            //o.Emission = (0,0,0);

            clip(yThreshold4 - IN.coord.y);

            clip(_ClippingNumber * step - IN.coord.x);
            clip(_VisibleMax.x - IN.coord.x);
            clip(_VisibleMax.y - IN.coord.y);
            clip(_VisibleMax.z - IN.coord.z);

            clip(IN.coord.x - _VisibleMin.x);
            clip(IN.coord.y - _VisibleMin.y);
            clip(IN.coord.z - _VisibleMin.z);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
