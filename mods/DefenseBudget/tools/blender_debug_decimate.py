import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="D:/source/DefenseBudget/mods/DefenseBudget/models/raw/savings_bond_raw.glb")
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
obj = meshes[0]
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
print("BEFORE:", len(obj.data.polygons), "users:", obj.data.users)
mod = obj.modifiers.new(name="Decimate", type='DECIMATE')
mod.decimate_type = 'COLLAPSE'
mod.ratio = 8000 / len(obj.data.polygons)
try:
    bpy.ops.object.modifier_apply(modifier="Decimate")
    print("apply ok")
except Exception as e:
    print("apply FAILED:", e)
print("AFTER:", len(obj.data.polygons))
