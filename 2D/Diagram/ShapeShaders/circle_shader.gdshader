shader_type spatial;
render_mode cull_disabled;

#include "sdf_primitives.gdshaderinc"
#include "sdf_common.gdshaderinc"

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float thickness : hint_range(0.01, 0.5) = 0.1;
uniform float inner_thickness : hint_range(0.01, 0.5) = 0.01;
uniform float smoothness : hint_range(0.001, 0.1) = 0.01;

// Shape-specific parameters
uniform vec2 shape_center = vec2(0.0, 0.0);  // Center in object space
uniform float radius = 1;

varying vec2 object_pos;

void vertex() {
    // Pass object-space position to fragment shader
    object_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xy;
}

void fragment() {
    // Use object-space coordinates instead of UV
    vec2 p = object_pos - shape_center;

    float d = sdCircle(p, radius);
    d = applyThickness(d, thickness, inner_thickness);

    // Apply smoothstep for antialiasing
    float alpha = 1.0 - smoothstep(0.0, smoothness, d);

    // Mix colors based on the SDF
    vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    float final_alpha = mix(background_color.a, shape_color.a, alpha);

    ALBEDO = color;
    ALPHA = final_alpha;
}
