Shader "Custom/HoleMask_Optimized"
{
    SubShader
    {
        // Vẽ thật sớm, trước Geometry mặc định
        Tags { "Queue" = "Geometry-10" "RenderType" = "Opaque" }

        Pass
        {
            // 1. Viết stencil mà không ghi depth
            ZWrite   Off
            ZTest    Always
            ColorMask 0

            // 2. Ghi stencil = 1 ở mọi pixel của cylinder (front & back)
            Cull     Off
            Stencil
            {
                Ref         1
                Comp        Always    // luôn luôn ghi
                Pass        Replace   // ghi giá trị Ref
                ReadMask    255
                WriteMask   255
            }

            // Không cần CGPROGRAM – chỉ dùng fixed‐function để ghi stencil
        }
    }
}
