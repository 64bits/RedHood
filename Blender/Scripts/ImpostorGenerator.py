import bpy
import math
from mathutils import Vector
import tempfile
import os

# ==================== CONFIGURATION ====================

# Object settings
TARGET_OBJECT_NAME = None  # None = Use selected
HORIZONTAL_ANGLES = 8      # Number of intersecting planes
VERTICAL_OFFSET = 0        

# Texture Quality
BASE_RESOLUTION = 512      # Resolution PER CELL (Atlas will be larger)
IMAGE_FORMAT = 'PNG'

# Output
SAVE_IMAGES = False        # If False, packs image into .blend
OUTPUT_PATH = "/tmp/"

# ==================== SCRIPT ====================

def get_tight_bounds_and_aspect(obj):
    """Calculates the tightest possible cylinder around the object's ORIGIN."""
    if obj.type != 'MESH':
        return 2.0, 2.0, -1.0, 0.0, 1.0

    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    
    min_z = min(c.z for c in corners)
    max_z = max(c.z for c in corners)
    height = max_z - min_z
    mid_z = (min_z + max_z) / 2.0

    spine_x = obj.location.x
    spine_y = obj.location.y
    
    max_radius_sq = 0
    for c in corners:
        dist_sq = (c.x - spine_x)**2 + (c.y - spine_y)**2
        if dist_sq > max_radius_sq:
            max_radius_sq = dist_sq
            
    max_radius = math.sqrt(max_radius_sq)
    width = max_radius * 2.0
    
    return width, height, min_z, mid_z, max_z

def setup_render_settings(scene, width, height):
    """Calculates resolution based on aspect ratio."""
    ratio = height / width
    
    if ratio > 1:
        res_x = BASE_RESOLUTION
        res_y = int(BASE_RESOLUTION * ratio)
    else:
        res_y = BASE_RESOLUTION
        res_x = int(BASE_RESOLUTION / ratio)
        
    # Ensure even numbers
    if res_x % 2 != 0: res_x += 1
    if res_y % 2 != 0: res_y += 1

    settings = {
        "engine": scene.render.engine,
        "light": scene.display.shading.light,
        "color_type": scene.display.shading.color_type,
        "shadows": scene.display.shading.show_shadows,
        "film": scene.render.film_transparent,
        "res_x": scene.render.resolution_x,
        "res_y": scene.render.resolution_y,
    }
    
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'FLAT'
    scene.display.shading.color_type = 'TEXTURE'
    scene.display.shading.show_shadows = False
    scene.render.film_transparent = True
    
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    
    return settings, res_x, res_y

def setup_ortho_camera(target_origin, mid_z, width, angle_deg):
    cam_data = bpy.data.cameras.new("ImpostorCamData")
    cam_obj = bpy.data.objects.new("ImpostorCam", cam_data)
    bpy.context.scene.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    cam_data.type = 'ORTHO'
    cam_data.sensor_fit = 'HORIZONTAL'
    cam_data.ortho_scale = width
    
    dist = width * 5.0 
    rad = math.radians(angle_deg)
    
    x = target_origin.x + dist * math.sin(rad)
    y = target_origin.y + dist * math.cos(rad)
    z = mid_z 
    
    cam_obj.location = Vector((x, y, z))
    
    look_target = Vector((target_origin.x, target_origin.y, mid_z))
    direction = look_target - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    return cam_obj

def generate_texture_atlas(image_paths, res_x, res_y, count):
    """
    Stitches rendered images into a single Texture Atlas.
    """
    print("--- Generating Texture Atlas ---")
    
    # Calculate Grid
    cols = math.ceil(math.sqrt(count))
    rows = math.ceil(count / cols)
    
    atlas_width = res_x * cols
    atlas_height = res_y * rows
    
    # Create blank image (Float buffer for precision)
    atlas_name = f"Atlas_Impostor"
    atlas_img = bpy.data.images.new(atlas_name, width=atlas_width, height=atlas_height, alpha=True)
    
    # Initialize a blank pixel list (R,G,B,A) flat array
    # 0.0 is transparent black
    total_pixels = atlas_width * atlas_height * 4
    atlas_pixels = [0.0] * total_pixels
    
    print(f"Atlas Size: {atlas_width}x{atlas_height} (Grid: {cols}x{rows})")
    
    for idx, path in enumerate(image_paths):
        # Determine Grid Position (Bottom-Left origin for easier UVs)
        col_idx = idx % cols
        row_idx = idx // cols
        
        # Load temp image
        try:
            src_img = bpy.data.images.load(path)
            # Access pixels (this is a simplified view)
            src_pixels = list(src_img.pixels)
            
            # Copy pixels row by row
            # Calculate offsets
            base_x = col_idx * res_x
            base_y = row_idx * res_y
            
            for y in range(res_y):
                # Extract row from source
                src_start = y * res_x * 4
                src_end = src_start + (res_x * 4)
                row_data = src_pixels[src_start:src_end]
                
                # Calculate target position in flattened atlas array
                # Atlas index = ((GlobalY) * AtlasWidth + GlobalX) * 4
                target_y = base_y + y
                target_start = (target_y * atlas_width + base_x) * 4
                
                # Insert row
                atlas_pixels[target_start : target_start + len(row_data)] = row_data
            
            # Cleanup source
            bpy.data.images.remove(src_img)
            
        except Exception as e:
            print(f"Failed to process atlas tile {idx}: {e}")
            
    # Assign pixels to new image
    atlas_img.pixels = atlas_pixels
    if SAVE_IMAGES:
        atlas_img.filepath_raw = os.path.join(OUTPUT_PATH, f"{atlas_name}.png")
        atlas_img.file_format = 'PNG'
        atlas_img.save()
    else:
        atlas_img.pack()
        
    return atlas_img, cols, rows

def create_intersecting_quads(name, atlas_img, cols, rows, origin, width, min_z, max_z):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    
    obj.location = origin
    
    verts = []
    faces = []
    uvs = []
    
    radius = width / 2.0
    z_top = max_z - origin.z
    z_btm = min_z - origin.z
    
    vert_idx = 0
    
    for i in range(HORIZONTAL_ANGLES):
        angle_deg = (i / HORIZONTAL_ANGLES) * 360
        angle_rad = math.radians(angle_deg)
        
        rx = math.cos(angle_rad) 
        ry = -math.sin(angle_rad)
        
        # Quad Vertices
        v0 = Vector((-rx * radius, -ry * radius, z_btm)) # BL
        v1 = Vector((rx * radius, ry * radius, z_btm))   # BR
        v2 = Vector((rx * radius, ry * radius, z_top))   # TR
        v3 = Vector((-rx * radius, -ry * radius, z_top)) # TL
        
        verts.extend([v0, v1, v2, v3])
        faces.append((vert_idx, vert_idx+1, vert_idx+2, vert_idx+3))
        
        # --- UV Calculation for Atlas ---
        col_idx = i % cols
        row_idx = i // cols
        
        # Calculate 0-1 range for this specific cell
        u_min = col_idx / cols
        u_max = (col_idx + 1) / cols
        v_min = row_idx / rows
        v_max = (row_idx + 1) / rows
        
        # Add slight padding to prevent bleeding (optional, usually needed for mipmaps)
        # Keeping simple for now
        
        # Order: BL, BR, TR, TL
        uvs.extend([
            (u_min, v_min),
            (u_max, v_min),
            (u_max, v_max),
            (u_min, v_max)
        ])
        
        vert_idx += 4
        
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    
    # Assign UVs
    mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    for idx, loop in enumerate(mesh.loops):
        uv_layer[idx].uv = uvs[idx]
        
    # Create ONE material for the atlas
    mat = create_material(atlas_img, f"Mat_{name}")
    obj.data.materials.append(mat)
        
    obj.visible_shadow = False
    return obj

def create_material(img, name):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    
    out = nodes.new('ShaderNodeOutputMaterial')
    tex = nodes.new('ShaderNodeTexImage')
    emit = nodes.new('ShaderNodeEmission')
    trans = nodes.new('ShaderNodeBsdfTransparent')
    mix = nodes.new('ShaderNodeMixShader')
    
    tex.image = img
    tex.interpolation = 'Linear' # or 'Closest' for pixel art
    
    mat.node_tree.links.new(tex.outputs['Color'], emit.inputs['Color'])
    mat.node_tree.links.new(tex.outputs['Alpha'], mix.inputs['Fac'])
    mat.node_tree.links.new(trans.outputs['BSDF'], mix.inputs[1])
    mat.node_tree.links.new(emit.outputs['Emission'], mix.inputs[2])
    mat.node_tree.links.new(mix.outputs['Shader'], out.inputs['Surface'])
    
    mat.blend_method = 'BLEND'
    mat.use_backface_culling = False
    return mat

def generate_impostor():
    if TARGET_OBJECT_NAME:
        target = bpy.data.objects.get(TARGET_OBJECT_NAME)
    elif bpy.context.selected_objects:
        target = bpy.context.selected_objects[0]
    else:
        print("Error: No object selected.")
        return

    # 1. Bounds & Aspect
    width, height, min_z, mid_z, max_z = get_tight_bounds_and_aspect(target)
    
    print(f"Target: {target.name}")
    print(f"Dimensions: {width:.2f} x {height:.2f}")
    
    # 2. Setup
    scene = bpy.context.scene
    old_settings, res_x, res_y = setup_render_settings(scene, width, height)
    
    original_vis = {}
    for o in bpy.data.objects:
        if o != target:
            original_vis[o] = o.hide_render
            o.hide_render = True
            
    temp_files = []
    
    # 3. Render Loop
    print(f"Rendering {HORIZONTAL_ANGLES} angles...")
    
    for i in range(HORIZONTAL_ANGLES):
        angle_deg = (i / HORIZONTAL_ANGLES) * 360
        
        cam = setup_ortho_camera(target.location, mid_z, width, angle_deg)
        
        # Use temp directory
        filename = f"temp_imp_{target.name}_{i:02d}.png"
        path = os.path.join(tempfile.gettempdir(), filename)
        scene.render.filepath = path
            
        bpy.ops.render.render(write_still=True)
        temp_files.append(path)
        
        bpy.data.objects.remove(cam)

    # 4. Generate Atlas
    atlas_img, cols, rows = generate_texture_atlas(temp_files, res_x, res_y, HORIZONTAL_ANGLES)

    # 5. Create Geometry
    star = create_intersecting_quads(
        f"Impostor_{target.name}",
        atlas_img,
        cols, 
        rows,
        target.location,
        width,
        min_z,
        max_z
    )
    
    # === FIX 1: ROTATE 180 DEGREES ON Z ===
    # We apply rotation to the object transform to flip it to the "correct" side
    star.rotation_euler.z = math.pi
    
    # 6. Cleanup
    scene.render.engine = old_settings['engine']
    scene.render.resolution_x = old_settings['res_x']
    scene.render.resolution_y = old_settings['res_y']
    scene.render.film_transparent = old_settings['film']
    
    for o, vis in original_vis.items():
        o.hide_render = vis
        
    # Cleanup temp files from disk
    for f in temp_files:
        try:
            os.remove(f)
        except:
            pass
        
    print(f"Done. Created {star.name}. Atlas: {cols}x{rows}")

if __name__ == "__main__":
    try:
        generate_impostor()
    except Exception as e:
        print(e)
        import traceback
        traceback.print_exc()