shader_type spatial;
render_mode cull_disabled;

#include "sdf_primitives.gdshaderinc"
#include "sdf_common.gdshaderinc"
#include "sdf_operations.gdshaderinc"

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float thickness : hint_range(0.01, 0.5) = 0.1;
uniform float inner_thickness : hint_range(0.01, 0.5) = 0.01;
uniform float smoothness : hint_range(0.001, 0.1) = 0.01;

// Composite parameters
uniform int operation = 0; // 0=Union, 1=SmoothUnion, 2=Subtraction, etc.
uniform float operation_smoothness = 0.1;

// Shape types
uniform int shape1_type = 0;
uniform int shape2_type = 0;

// Shape 1 parameters
uniform vec2 shape1_shape_center = vec2(0.0, 0.0);
uniform float shape1_radius = 0.5;
uniform vec2 shape1_size = vec2(1.0, 0.5);
uniform vec2 shape1_point_a = vec2(-1.0, 0.0);
uniform vec2 shape1_point_b = vec2(1.0, 0.0);
uniform vec2 shape1_point_c = vec2(0.0, 1.0);
uniform vec2 shape1_arrow_start = vec2(-1.0, 0.0);
uniform vec2 shape1_arrow_end = vec2(1.0, 0.0);
uniform float shape1_head_length = 0.3;
uniform float shape1_head_angle = 30.0;

// Shape 2 parameters
uniform vec2 shape2_shape_center = vec2(0.0, 0.0);
uniform float shape2_radius = 0.5;
uniform vec2 shape2_size = vec2(1.0, 0.5);
uniform vec2 shape2_point_a = vec2(-1.0, 0.0);
uniform vec2 shape2_point_b = vec2(1.0, 0.0);
uniform vec2 shape2_point_c = vec2(0.0, 1.0);
uniform vec2 shape2_arrow_start = vec2(-1.0, 0.0);
uniform vec2 shape2_arrow_end = vec2(1.0, 0.0);
uniform float shape2_head_length = 0.3;
uniform float shape2_head_angle = 30.0;

varying vec2 object_pos;

void vertex() {
    object_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xy;
}

float evaluateShape(int shape_type, vec2 p, int shape_index) {
    if (shape_index == 1) {
        if (shape_type == 0) { // Circle
            return sdCircle(p - shape1_shape_center, shape1_radius);
        } else if (shape_type == 1) { // Rectangle
            return sdBox(p - shape1_shape_center, shape1_size);
        } else if (shape_type == 2) { // Line
            return sdLine(p, shape1_point_a, shape1_point_b);
        } else if (shape_type == 3) { // Triangle
            return sdTriangle(p, shape1_point_a, shape1_point_b, shape1_point_c);
        } else if (shape_type == 4) { // Arrow
            return sdArrow(p, shape1_arrow_start, shape1_arrow_end, shape1_head_length, shape1_head_angle);
        }
    } else {
        if (shape_type == 0) { // Circle
            return sdCircle(p - shape2_shape_center, shape2_radius);
        } else if (shape_type == 1) { // Rectangle
            return sdBox(p - shape2_shape_center, shape2_size);
        } else if (shape_type == 2) { // Line
            return sdLine(p, shape2_point_a, shape2_point_b);
        } else if (shape_type == 3) { // Triangle
            return sdTriangle(p, shape2_point_a, shape2_point_b, shape2_point_c);
        } else if (shape_type == 4) { // Arrow
            return sdArrow(p, shape2_arrow_start, shape2_arrow_end, shape2_head_length, shape2_head_angle);
        }
    }
    return 1000.0; // Large distance for unknown shape
}

float applyOperation(float d1, float d2, int op, float k) {
    if (op == 0) { // Union
        return opUnion(d1, d2);
    } else if (op == 1) { // SmoothUnion
        return opSmoothUnion(d1, d2, k);
    } else if (op == 2) { // Subtraction
        return opSubtraction(d1, d2);
    } else if (op == 3) { // SmoothSubtraction
        return opSmoothSubtraction(d1, d2, k);
    } else if (op == 4) { // Intersection
        return opIntersection(d1, d2);
    } else if (op == 5) { // SmoothIntersection
        return opSmoothIntersection(d1, d2, k);
    }
    return d1;
}

void fragment() {
    vec2 p = object_pos;
    
    // Evaluate both shapes
    float d1 = evaluateShape(shape1_type, p, 1);
    float d2 = evaluateShape(shape2_type, p, 2);
    
    // Apply operation
    float d = applyOperation(d1, d2, operation, operation_smoothness);
    
    // Apply thickness
    d = applyThickness(d, thickness, inner_thickness);
    
    // Apply smoothstep for antialiasing
    float alpha = 1.0 - smoothstep(0.0, smoothness, d);
    
    // Mix colors based on the SDF
    vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    float final_alpha = mix(background_color.a, shape_color.a, alpha);
    
    ALBEDO = color;
    ALPHA = final_alpha;
}
