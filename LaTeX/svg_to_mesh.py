import bpy
from mathutils import Vector
import math
import sys

SCALE_FACTOR = 1180

# Get the SVG file path from the arguments
argv = sys.argv
argv = argv[argv.index("--") + 1:]  # Get all args after "--"
svg_path = argv[0]
destination_path = argv[1]

# Clear existing objects
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# Import SVG
bpy.ops.import_curve.svg(filepath=svg_path)
bpy.ops.object.select_all(action='SELECT')


mesh_objects = []
for obj in bpy.context.selected_objects:
    mesh_objects.append(obj)
    bpy.context.view_layer.objects.active = obj
    # Commented out because mesh conversion happens automatically with gltf export
    # bpy.ops.object.convert(target='MESH')
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
 
# Move the first object to origin
if mesh_objects:
    first_obj = mesh_objects[0]
    second_obj = mesh_objects[1]
    
    # Calculate lower right corner of bounding box
    first_bbox_corners = [first_obj.matrix_world @ Vector(corner) for corner in first_obj.bound_box]
    second_bbox_corners = [second_obj.matrix_world @ Vector(corner) for corner in second_obj.bound_box]
    lower_right_corner = (min(corner.x for corner in second_bbox_corners),
                          min(corner.y for corner in first_bbox_corners),
                          min(corner.z for corner in first_bbox_corners))
    
    translation = -Vector(lower_right_corner)

    # Apply the same translation to all other objects
    for obj in mesh_objects:
        obj.location += translation
        obj.location *= SCALE_FACTOR
        obj.scale *= SCALE_FACTOR
        # obj.rotation_euler = (3.14159265358 / 2, 0, 0) #This will only work if we rotate around the baseline instead of center
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


    # # Create new empty object at the origin to parent all objects to
    # for obj in mesh_objects:
    #     # Set the parent while keeping world space location
    #
    # # Rotate the parent by 90 degrees around the x-axis
    #
    # for obj in mesh_objects:
    #     # remove the relationship why keeping world space location
    #
    # # Destroy the parent object

    # Create a new empty object at the origin to parent all objects to
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    parent_object = bpy.context.object
    
    for obj in mesh_objects:
        # Set the parent while keeping world space location
        obj.select_set(True)
        parent_object.select_set(True)
        bpy.context.view_layer.objects.active = parent_object
        bpy.ops.object.parent_set(type='OBJECT', keep_transform=True)
    
    # Rotate the parent by 90 degrees around the x-axis
    parent_object.rotation_euler[0] += math.radians(90)
    
    # Update the scene to apply transformation
    # bpy.context.view_layer.update()

    for obj in mesh_objects:
        # Remove the parent while keeping world space location
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
        # This is not allowed because the objects are curves, which are fundamentally 2D.
        bpy.ops.object.convert(target='MESH')
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

    # Destroy the parent object
    bpy.data.objects.remove(parent_object)

bpy.data.objects.remove(first_obj, do_unlink=True)

# Export to GLTF
bpy.ops.export_scene.gltf(filepath=destination_path, export_format='GLTF_EMBEDDED')

print("Exported mesh to GLTF")
