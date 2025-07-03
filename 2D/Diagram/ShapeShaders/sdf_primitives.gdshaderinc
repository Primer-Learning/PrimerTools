// SDF primitive functions

float sdCircle(vec2 p, float r) {
    return length(p) - r;
}

float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

float sdHexagon(vec2 p, float r) {
    const vec3 k = vec3(-0.866025404, 0.5, 0.577350269);
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= vec2(clamp(p.x, -k.z * r, k.z * r), r);
    return length(p) * sign(p.y);
}

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

float sdLine(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) * 2.0;
}

float sdCapsule(vec2 p, vec2 a, vec2 b, float r) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

// Uneven capsule - capsule with different radii at each end
float sdUnevenCapsule(vec2 p, vec2 a, vec2 b, float ra, float rb) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = length(ba);
    vec2 ba_norm = ba / h;

    // Transform to local coordinates where capsule is vertical
    p = vec2(dot(pa, vec2(ba_norm.y, -ba_norm.x)), dot(pa, ba_norm));
    p.x = abs(p.x);

    float b_param = (ra - rb) / h;
    float a_param = sqrt(1.0 - b_param * b_param);
    float k = dot(p, vec2(-b_param, a_param));

    if (k < 0.0) return length(p) - ra;
    if (k > a_param * h) return length(p - vec2(0.0, h)) - rb;
    return dot(p, vec2(a_param, b_param)) - ra;
}

float sdArrow(vec2 p, vec2 start, vec2 end, float head_len, float head_angle_deg) {
    // Arrow shaft (line)
    float shaft = sdLine(p, start, end);

    // Calculate arrow direction
    vec2 dir = normalize(end - start);
    vec2 perp = vec2(-dir.y, dir.x);

    // Convert angle to radians
    float angle_rad = radians(head_angle_deg);
    float head_width = head_len * tan(angle_rad);

    // Calculate arrowhead points
    vec2 head_base = end - dir * head_len;
    vec2 wing1 = head_base + perp * head_width;
    vec2 wing2 = head_base - perp * head_width;

    // Arrowhead as two lines
    float head1 = sdLine(p, end, wing1);
    float head2 = sdLine(p, end, wing2);

    // Combine all parts
    return min(shaft, min(head1, head2));
}

// Quadratic bezier curve SDF
float sdBezierQuadratic( in vec2 pos, in vec2 A, in vec2 B, in vec2 C )
{
    vec2 a = B - A;
    vec2 b = A - 2.0*B + C;
    vec2 c = a * 2.0;
    vec2 d = A - pos;
    float kk = 1.0/dot(b,b);
    float kx = kk * dot(a,b);
    float ky = kk * (2.0*dot(a,a)+dot(d,b)) / 3.0;
    float kz = kk * dot(d,a);
    float res = 0.0;
    float p = ky - kx*kx;
    float p3 = p*p*p;
    float q = kx*(2.0*kx*kx-3.0*ky) + kz;
    float h = q*q + 4.0*p3;
    if( h >= 0.0)
    {
        h = sqrt(h);
        vec2 x = (vec2(h,-h)-q)/2.0;
        vec2 uv = sign(x)*pow(abs(x), vec2(1.0/3.0));
        float t = clamp( uv.x+uv.y-kx, 0.0, 1.0 );
        res = dot(d + (c + b*t)*t, d + (c + b*t)*t);
    }
    else
    {
        float z = sqrt(-p);
        float v = acos( q/(p*z*2.0) ) / 3.0;
        float m = cos(v);
        float n = sin(v)*1.732050808;
        vec3  t = clamp(vec3(m+m,-n-m,n-m)*z-kx,0.0,1.0);
        res = min( dot(d+(c+b*t.x)*t.x, d+(c+b*t.x)*t.x),
                   dot(d+(c+b*t.y)*t.y, d+(c+b*t.y)*t.y) );
        // the third root cannot be the closest
        // res = min(res,dot2(d+(c+b*t.z)*t.z));
    }
    return sqrt( res ) * 2.0;
}

// Cubic bezier curve SDF (approximation)
float sdBezierCubic(vec2 p, vec2 p0, vec2 p1, vec2 p2, vec2 p3) {
    // This is an approximation using subdivision
    const int SUBDIVISIONS = 64	;
    float min_dist = 1e10;

    vec2 prev = p0;
    for (int i = 1; i <= SUBDIVISIONS; i++) {
        float t = float(i) / float(SUBDIVISIONS);
        float t2 = t * t;
        float t3 = t2 * t;
        float mt = 1.0 - t;
        float mt2 = mt * mt;
        float mt3 = mt2 * mt;

        vec2 point = mt3 * p0 + 3.0 * mt2 * t * p1 + 3.0 * mt * t2 * p2 + t3 * p3;

        // Distance to line segment
        vec2 pa = p - prev;
        vec2 ba = point - prev;
        float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
        float d = length(pa - ba * h);

        min_dist = min(min_dist, d);
        prev = point;
    }

    return min_dist * 2.0;
}

// Curly bracket SDF using two cubic bezier curves
float sdCurlyBracket(vec2 p, vec2 tip1, vec2 tip2, vec2 stem) {
	vec2 tip_line = tip2 - tip1;
	vec2 tip_to_stem = stem - tip1;
	vec2 closest_point = tip1 + dot(tip_to_stem, tip_line) / dot(tip_line, tip_line) * tip_line;
	vec2 dir = closest_point - stem;

    // Calculate control points for the first half (stem to tip1)
    // The control points create the characteristic S-curve of a curly bracket
    vec2 p1_0 = stem; // Start at stem
    vec2 p1_1 = closest_point; // Pull in direction of bracket
    vec2 p1_2 = tip1 - dir; // Pull back before tip
    vec2 p1_3 = tip1; // End at first tip

    // Calculate control points for the second half (stem to tip2)
    vec2 p2_0 = stem; // Start at stem
    vec2 p2_1 = closest_point; // Pull in direction of bracket
    vec2 p2_2 = tip2 - dir; // Pull back before tip
    vec2 p2_3 = tip2; // End at second tip

    // Calculate distance to both bezier curves
    float d1 = sdBezierCubic(p, p1_0, p1_1, p1_2, p1_3);
    float d2 = sdBezierCubic(p, p2_0, p2_1, p2_2, p2_3);

    // Return the minimum distance (union of both curves)
    return min(d1, d2);
}

// Cubic bezier curve SDF with variable thickness
float sdBezierCubicVariable(vec2 p, vec2 p0, vec2 p1, vec2 p2, vec2 p3, float base_thickness, float thickness_variation) {
    // This uses subdivision with uneven capsules for smooth transitions
    const int SUBDIVISIONS = 64;
    float min_dist = 1e10;

    vec2 prev = p0;
    float prev_t = 0.0;
    float prev_thickness = base_thickness; // Thin at start

    for (int i = 1; i <= SUBDIVISIONS; i++) {
        float t = float(i) / float(SUBDIVISIONS);
        float t2 = t * t;
        float t3 = t2 * t;
        float mt = 1.0 - t;
        float mt2 = mt * mt;
        float mt3 = mt2 * mt;

        vec2 point = mt3 * p0 + 3.0 * mt2 * t * p1 + 3.0 * mt * t2 * p2 + t3 * p3;

        // Calculate thickness at current point
        // Thickness function: thin at ends (t=0,1), thick in middle (t=0.5)
        float curr_thickness = base_thickness * (1.0 + thickness_variation * sin(3.14159 * t));

        // Distance to uneven capsule with different radii at each end
        float d = sdUnevenCapsule(p, prev, point, prev_thickness, curr_thickness);

        min_dist = min(min_dist, d);
        prev = point;
        prev_t = t;
        prev_thickness = curr_thickness;
    }

    return min_dist;
}

// Quadratic bezier curve SDF with variable thickness
float sdBezierQuadraticVariable(vec2 pos, vec2 A, vec2 B, vec2 C, float base_thickness, float thickness_variation) {
    // First find the closest point on the curve (existing algorithm)
    vec2 a = B - A;
    vec2 b = A - 2.0*B + C;
    vec2 c = a * 2.0;
    vec2 d = A - pos;
    float kk = 1.0/dot(b,b);
    float kx = kk * dot(a,b);
    float ky = kk * (2.0*dot(a,a)+dot(d,b)) / 3.0;
    float kz = kk * dot(d,a);
    float res = 0.0;
    float t = 0.5; // Default to middle if calculation fails

    float p = ky - kx*kx;
    float p3 = p*p*p;
    float q = kx*(2.0*kx*kx-3.0*ky) + kz;
    float h = q*q + 4.0*p3;
    if( h >= 0.0)
    {
        h = sqrt(h);
        vec2 x = (vec2(h,-h)-q)/2.0;
        vec2 uv = sign(x)*pow(abs(x), vec2(1.0/3.0));
        t = clamp( uv.x+uv.y-kx, 0.0, 1.0 );
        res = dot(d + (c + b*t)*t, d + (c + b*t)*t);
    }
    else
    {
        float z = sqrt(-p);
        float v = acos( q/(p*z*2.0) ) / 3.0;
        float m = cos(v);
        float n = sin(v)*1.732050808;
        vec3  tv = clamp(vec3(m+m,-n-m,n-m)*z-kx,0.0,1.0);
        vec2 qx = d+(c+b*tv.x)*tv.x;
        vec2 qy = d+(c+b*tv.y)*tv.y;
        if (dot(qx, qx) < dot(qy, qy)) {
            res = dot(qx, qx);
            t = tv.x;
        } else {
            res = dot(qy, qy);
            t = tv.y;
        }
    }

    // Calculate thickness based on position along curve
    float thickness = base_thickness * (1.0 + thickness_variation * sin(3.14159 * t));

    return sqrt(res) - thickness;
}

// Curly bracket SDF with variable thickness
float sdCurlyBracketVariable(vec2 p, vec2 tip1, vec2 tip2, vec2 stem, float base_thickness, float thickness_variation) {
    vec2 tip_line = tip2 - tip1;
    vec2 tip_to_stem = stem - tip1;
    vec2 closest_point = tip1 + dot(tip_to_stem, tip_line) / dot(tip_line, tip_line) * tip_line;
    vec2 dir = closest_point - stem;

    // Calculate control points for the first half (stem to tip1)
    vec2 p1_0 = stem; // Start at stem
    vec2 p1_1 = closest_point; // Pull in direction of bracket
    vec2 p1_2 = tip1 - dir; // Pull back before tip
    vec2 p1_3 = tip1; // End at first tip

    // Calculate control points for the second half (stem to tip2)
    vec2 p2_0 = stem; // Start at stem
    vec2 p2_1 = closest_point; // Pull in direction of bracket
    vec2 p2_2 = tip2 - dir; // Pull back before tip
    vec2 p2_3 = tip2; // End at second tip

    // Calculate distance to both bezier curves with variable thickness
    float d1 = sdBezierCubicVariable(p, p1_0, p1_1, p1_2, p1_3, base_thickness, thickness_variation);
    float d2 = sdBezierCubicVariable(p, p2_0, p2_1, p2_2, p2_3, base_thickness, thickness_variation);

    // Return the minimum distance (union of both curves)
    return min(d1, d2);
}
