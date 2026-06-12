# Headless Blender script: clean raw TRELLIS.2 GLBs and export OBJ + raw RGBA texture
# for the mod's runtime loader. Based on the cleanup procedure in D:/tools/3d-pipeline/README.md.
# Run: blender --background --python blender_clean_export.py [-- item1 item2 ...]
import bpy
import bmesh
import os
import sys
import numpy as np

RAW = "D:/source/DefenseBudget/mods/AuditDepartment/models/raw"
OUT = "D:/source/DefenseBudget/mods/AuditDepartment/AuditDepartment/models"
TARGET_FACES = 8000
TEX_SIZE = 512

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
items = argv or [f[:-8] for f in os.listdir(RAW) if f.endswith("_raw.glb")]
os.makedirs(OUT, exist_ok=True)

def bake_decimate(obj, target):
    current = len(obj.data.polygons)
    if current <= target:
        return
    mod = obj.modifiers.new(name="Decimate", type='DECIMATE')
    mod.decimate_type = 'COLLAPSE'
    mod.ratio = target / current
    mod.use_collapse_triangulate = True
    depsgraph = bpy.context.evaluated_depsgraph_get()
    baked = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph))
    obj.modifiers.clear()
    obj.data = baked
    print(f"   decimate {current} -> {len(obj.data.polygons)} (target {target})")

def clean_item(item):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=f"{RAW}/{item}_raw.glb")

    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    if not meshes:
        print(f"!! no mesh in {item}")
        return False
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active

    # decimate the raw mesh FIRST — once welded, the merged TRELLIS surface patches
    # become non-manifold and collapse decimation stalls
    bake_decimate(obj, TARGET_FACES * 2)

    # weld fragments
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=0.0005)
    bpy.ops.object.mode_set(mode='OBJECT')

    # NOTE: no largest-component filter or hole filling — those steps exist for rigging
    # (UniRig), which pickups skip. Display meshes tolerate floaters and open edges.

    # finishing decimate pass (may stall on non-manifold geometry; close enough is fine)
    bake_decimate(obj, TARGET_FACES)

    # drop loose vertices left behind by decimation (halves the OBJ size)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()

    bpy.ops.object.shade_smooth()

    # center origin for a predictable pivot
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    obj.location = (0, 0, 0)

    # extract baked texture -> raw RGBA32, bottom-up rows (matches Unity + OBJ vt)
    image = next((i for i in bpy.data.images
                  if i.name not in ('Render Result', 'Viewer Node') and i.size[0] > 0), None)
    if image:
        image.scale(TEX_SIZE, TEX_SIZE)
        pixels = np.array(image.pixels[:], dtype=np.float32)
        raw = (np.clip(pixels, 0.0, 1.0) * 255.0).astype(np.uint8).tobytes()
        with open(f"{OUT}/{item}.rgba", "wb") as f:
            f.write(raw)
        print(f"   texture: {item}.rgba {len(raw)} bytes")
    else:
        print(f"!! no texture found for {item}")

    # export OBJ (Z-up -> Y-up handled by default axes)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.wm.obj_export(
        filepath=f"{OUT}/{item}.obj",
        export_selected_objects=True,
        export_triangulated_mesh=True,
        export_materials=False,
        export_normals=True,
        export_uv=True,
    )
    print(f"   mesh: {item}.obj faces={len(obj.data.polygons)} verts={len(obj.data.vertices)}")
    render_preview(obj, item)
    return True

def render_preview(obj, item):
    import mathutils
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    bbox = [obj.matrix_world @ mathutils.Vector(c) for c in obj.bound_box]
    center = sum(bbox, mathutils.Vector()) / 8
    size = max(max(v[i] for v in bbox) - min(v[i] for v in bbox) for i in range(3))
    cam.location = center + mathutils.Vector((size * 1.7, -size * 1.7, size * 1.2))
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'TEXTURE'
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.filepath = f"D:/source/DefenseBudget/mods/AuditDepartment/models/clean/{item}_preview.png"
    bpy.ops.render.render(write_still=True)

ok = 0
for item in items:
    print(f"=== cleaning {item} ===")
    try:
        if clean_item(item):
            ok += 1
    except Exception as e:
        print(f"!! FAILED {item}: {e}")
print(f"DONE {ok}/{len(items)}")
