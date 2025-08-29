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
container_objects = []
for obj in bpy.context.selected_objects:
    mesh_objects.append(obj)
    bpy.context.view_layer.objects.active = obj
    # Commented out because mesh conversion happens automatically with gltf export
    # bpy.ops.object.convert(target='MESH')
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    
    # Create an empty container for this mesh object
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=obj.location)
    container = bpy.context.object
    container.name = f"{obj.name}_container"
    container_objects.append(container)
    
    # Parent the mesh to its container
    obj.parent = container
    obj.parent_type = 'OBJECT'
    # Reset the mesh's transform since it's now relative to the container
    obj.location = (0, 0, 0)
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
 
# Move the first container to origin
if container_objects:
    first_container = container_objects[0]
    second_container = container_objects[1]
    first_obj = mesh_objects[0]
    second_obj = mesh_objects[1]
    
    # Calculate lower right corner of bounding box
    first_bbox_corners = [first_obj.matrix_world @ Vector(corner) for corner in first_obj.bound_box]
    second_bbox_corners = [second_obj.matrix_world @ Vector(corner) for corner in second_obj.bound_box]
    lower_right_corner = (min(corner.x for corner in second_bbox_corners),
                          min(corner.y for corner in first_bbox_corners),
                          min(corner.z for corner in first_bbox_corners))
    
    translation = -Vector(lower_right_corner)

    # Apply the same translation to all container objects
    for container in container_objects:
        container.location += translation
        container.location *= SCALE_FACTOR
        container.scale *= SCALE_FACTOR
        # container.rotation_euler = (3.14159265358 / 2, 0, 0) #This will only work if we rotate around the baseline instead of center
        bpy.ops.object.select_all(action='DESELECT')
        container.select_set(True)
        bpy.context.view_layer.objects.active = container
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

    # Create a new empty object at the origin to parent all containers to
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    parent_object = bpy.context.object
    
    for container in container_objects:
        # Set the parent while keeping world space location
        container.select_set(True)
        parent_object.select_set(True)
        bpy.context.view_layer.objects.active = parent_object
        bpy.ops.object.parent_set(type='OBJECT', keep_transform=True)
    
    # Rotate the parent by 90 degrees around the x-axis
    parent_object.rotation_euler[0] += math.radians(90)
    
    # Update the scene to apply transformation
    # bpy.context.view_layer.update()

    for container in container_objects:
        # Remove the parent while keeping world space location
        container.select_set(True)
        bpy.context.view_layer.objects.active = container
        bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

    # Convert mesh objects to actual meshes (they're still curves at this point)
    for obj in mesh_objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        # This is not allowed on curves until they're converted
        bpy.ops.object.convert(target='MESH')

    # Destroy the parent object
    bpy.data.objects.remove(parent_object)

# Remove the first container and its mesh
bpy.data.objects.remove(first_obj, do_unlink=True)
bpy.data.objects.remove(first_container, do_unlink=True)

# Export to GLTF
bpy.ops.export_scene.gltf(filepath=destination_path, export_format='GLTF_EMBEDDED')

print("Exported mesh to GLTF")
