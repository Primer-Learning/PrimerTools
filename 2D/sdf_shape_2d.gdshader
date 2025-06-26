shader_type spatial;
render_mode cull_disabled;

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float thickness : hint_range(0.01, 0.5) = 0.1;
uniform float smoothness : hint_range(0.001, 0.1) = 0.01;
uniform int shape_type : hint_range(0, 4) = 0; // 0: circle, 1: box, 2: hexagon, 3: star, 4: triangle
uniform vec2 triangle_p1 = vec2(0.0, -0.7);
uniform vec2 triangle_p2 = vec2(-0.6, 0.5);
uniform vec2 triangle_p3 = vec2(0.6, 0.5);

// SDF for a circle
float sdCircle(vec2 p, float r) {
    return length(p) - r;
}

// SDF for a box
float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

// SDF for a hexagon
float sdHexagon(vec2 p, float r) {
    const vec3 k = vec3(-0.866025404, 0.5, 0.577350269);
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= vec2(clamp(p.x, -k.z * r, k.z * r), r);
    return length(p) * sign(p.y);
}

// SDF for a 5-pointed star
float sdStar(vec2 p, float r, float n, float m) {
    float an = 3.141593 / n;
    float en = 3.141593 / m;
    vec2 acs = vec2(cos(an), sin(an));
    vec2 ecs = vec2(cos(en), sin(en));
    
    float bn = mod(atan(p.x, p.y), 2.0 * an) - an;
    p = length(p) * vec2(cos(bn), abs(sin(bn)));
    p -= r * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, r * acs.y / ecs.y);
    return length(p) * sign(p.x);
}

// SDF for a triangle with arbitrary vertices
float sdTriangle(vec2 p, vec2 a, vec2 b, vec2 c) {
    vec2 e0 = b - a;
    vec2 e1 = c - b;
    vec2 e2 = a - c;
    
    vec2 v0 = p - a;
    vec2 v1 = p - b;
    vec2 v2 = p - c;
    
    vec2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    vec2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    vec2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
    
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    vec2 d = min(min(vec2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                     vec2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                     vec2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
    
    return -sqrt(d.x) * sign(d.y);
}

// Create hollow shape by using absolute value of SDF
float makeHollow(float d, float thick) {
    return abs(d) - thick;
}

void fragment() {
    // Convert UV coordinates to centered coordinates (-1 to 1)
    vec2 uv = UV * 2.0 - 1.0;
    
    float d;
    
    // Select shape based on uniform
    if (shape_type == 0) {
        d = sdCircle(uv, 0.7);
    } else if (shape_type == 1) {
        d = sdBox(uv, vec2(0.6, 0.6));
    } else if (shape_type == 2) {
        d = sdHexagon(uv, 0.7);
    } else if (shape_type == 3) {
        d = sdStar(uv, 0.7, 5.0, 2.0);
    } else {
        d = sdTriangle(uv, triangle_p1, triangle_p2, triangle_p3);
    }
    
    // Make the shape hollow
    d = makeHollow(d, thickness);
    
    // Apply smoothstep for antialiasing
    float alpha = 1.0 - smoothstep(0.0, smoothness, d);
    
    // Mix colors based on the SDF
    vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    float final_alpha = mix(background_color.a, shape_color.a, alpha);
    
    ALBEDO = color;
    ALPHA = final_alpha;
}