// Common SDF utilities

float addOuterThickness(float d, float thick) {
    return d - thick;
}

float makeHollow(float d, float thick) {
    return -(d + thick);
}

// Create hollow shape by using absolute value of SDF
float makeHollowAbs(float d, float thick) {
    return abs(d) - thick;
}

// Apply thickness based on whether we're inside or outside the shape
float applyThickness(float d, float outer_thick, float inner_thick) {
    if (d < 0.0) {
        return makeHollow(d, inner_thick);
    } else {
        return addOuterThickness(d, outer_thick);
    }
}
