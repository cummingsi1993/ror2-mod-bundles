using RoR2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefenseBudget
{
    // Shared asset plumbing: embedded sprite/texture loading, runtime OBJ meshes for
    // pickup models, and the publicized-assembly-safe item tier setter.
    internal static class Assets
    {
        private static GameObject prefabHolder;

        // Runtime "prefabs" live under an inactive holder so the objects themselves stay
        // active — instantiated pickups then spawn active.
        internal static Transform PrefabHolder
        {
            get
            {
                if (!prefabHolder)
                {
                    prefabHolder = new GameObject("DefenseBudgetPrefabHolder");
                    UnityEngine.Object.DontDestroyOnLoad(prefabHolder);
                    prefabHolder.SetActive(false);
                }
                return prefabHolder.transform;
            }
        }

        // The ItemDef.tier setter resolves through ItemTierCatalog, which is still empty
        // during plugin Awake — set the private deprecatedTier directly (reflection, because
        // it is publicized at compile time but private at runtime), then resolve the real
        // ItemTierDef once catalogs load.
        internal static void SetItemTier(ItemDef def, ItemTier tier)
        {
            typeof(ItemDef)
                .GetField("deprecatedTier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(def, tier);
            RoR2Application.onLoad += () =>
            {
                var tierDef = ItemTierCatalog.GetItemTierDef(tier);
                if (tierDef)
                {
                    typeof(ItemDef)
                        .GetField("_itemTierDef", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.SetValue(def, tierDef);
                }
            };
        }

        internal static ItemDef CreateItemDef(string name, string tokenBase, ItemTier tier, Sprite icon, GameObject model, ItemTag[] tags, bool canRemove = true)
        {
            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.name = name;
            def.nameToken = tokenBase + "_NAME";
            def.pickupToken = tokenBase + "_PICKUP";
            def.descriptionToken = tokenBase + "_DESC";
            def.loreToken = tokenBase + "_LORE";
            def.canRemove = canRemove;
            def.hidden = false;
            def.tags = tags;
            def.pickupIconSprite = icon;
#pragma warning disable CS0618
            def.pickupModelPrefab = model;
#pragma warning restore CS0618
            SetItemTier(def, tier);
            return def;
        }

        private static byte[] ReadResource(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        internal static Texture2D LoadRawTexture(string resourceName, int size)
        {
            try
            {
                var bytes = ReadResource(resourceName);
                if (bytes == null || bytes.Length != size * size * 4)
                {
                    return null;
                }
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                };
                texture.LoadRawTextureData(bytes);
                texture.Apply();
                return texture;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load texture '{resourceName}': {e}");
                return null;
            }
        }

        internal static Sprite LoadSprite(string resourceName, int size)
        {
            var texture = LoadRawTexture(resourceName, size);
            return texture
                ? Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f))
                : null;
        }

        // Clone a vanilla pickup material so meshes use a shader that works in RoR2's
        // deferred pipeline, then swap in our own texture/tint.
        internal static Material CreatePickupMaterial(Color color, Texture2D mainTexture, Color emission)
        {
            var coin = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarCoin/PickupLunarCoin.prefab").WaitForCompletion();
            var coinRenderer = coin ? coin.GetComponentInChildren<Renderer>(true) : null;
            var material = coinRenderer && coinRenderer.sharedMaterial
                ? new Material(coinRenderer.sharedMaterial)
                : new Material(Shader.Find("Standard"));
            foreach (var textureProperty in new[] { "_MainTex", "_NormalTex", "_BumpMap", "_EmTex", "_EmissionMap" })
            {
                if (material.HasProperty(textureProperty))
                {
                    material.SetTexture(textureProperty, textureProperty == "_MainTex" ? (Texture)(mainTexture ? mainTexture : Texture2D.whiteTexture) : null);
                }
            }
            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }
            if (material.HasProperty("_EmColor"))
            {
                material.SetColor("_EmColor", emission);
            }
            return material;
        }

        // Minimal OBJ parser (v/vt/vn + triangulated-or-fan f lines) producing a Unity Mesh.
        internal static Mesh LoadObjMesh(string resourceName)
        {
            var bytes = ReadResource(resourceName);
            if (bytes == null)
            {
                return null;
            }
            var positions = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var objNormals = new List<Vector3>();
            var vertexMap = new Dictionary<(int, int, int), int>();
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            int ResolveVertex(string token)
            {
                var parts = token.Split('/');
                int vi = int.Parse(parts[0], CultureInfo.InvariantCulture);
                int ti = parts.Length > 1 && parts[1].Length > 0 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
                int ni = parts.Length > 2 && parts[2].Length > 0 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 0;
                if (vi < 0) vi += positions.Count + 1;
                if (ti < 0) ti += texCoords.Count + 1;
                if (ni < 0) ni += objNormals.Count + 1;
                var key = (vi, ti, ni);
                if (vertexMap.TryGetValue(key, out int index))
                {
                    return index;
                }
                index = vertices.Count;
                vertices.Add(positions[vi - 1]);
                uvs.Add(ti > 0 ? texCoords[ti - 1] : Vector2.zero);
                normals.Add(ni > 0 ? objNormals[ni - 1] : Vector3.up);
                vertexMap[key] = index;
                return index;
            }

            using (var reader = new StreamReader(new MemoryStream(bytes)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        continue;
                    }
                    switch (parts[0])
                    {
                        case "v":
                            // negate X: OBJ is right-handed, Unity left-handed
                            positions.Add(new Vector3(
                                -float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture),
                                float.Parse(parts[3], CultureInfo.InvariantCulture)));
                            break;
                        case "vt":
                            texCoords.Add(new Vector2(
                                float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture)));
                            break;
                        case "vn":
                            objNormals.Add(new Vector3(
                                -float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture),
                                float.Parse(parts[3], CultureInfo.InvariantCulture)));
                            break;
                        case "f":
                            // fan-triangulate; reverse winding for the handedness flip
                            int first = ResolveVertex(parts[1]);
                            int previous = ResolveVertex(parts[2]);
                            for (int i = 3; i < parts.Length; i++)
                            {
                                int current = ResolveVertex(parts[i]);
                                triangles.Add(first);
                                triangles.Add(current);
                                triangles.Add(previous);
                                previous = current;
                            }
                            break;
                    }
                }
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Build a pickup model GameObject from embedded OBJ + raw RGBA texture resources.
        // Falls back to the provided factory (or the vanilla mystery model) if resources
        // are missing, so the mod stays functional before/without generated models.
        internal static GameObject CreatePickupModel(string name, string objResource, string textureResource, int textureSize, float targetSize, Func<GameObject> fallback = null)
        {
            try
            {
                var mesh = LoadObjMesh(objResource);
                if (mesh == null)
                {
                    Log.Warning($"Model resource '{objResource}' missing; using fallback model for {name}.");
                    return fallback != null ? fallback() : MysteryModel();
                }
                var texture = LoadRawTexture(textureResource, textureSize);
                var model = new GameObject(name);
                model.transform.SetParent(PrefabHolder);
                model.AddComponent<MeshFilter>().mesh = mesh;
                model.AddComponent<MeshRenderer>().material = CreatePickupMaterial(Color.white, texture, Color.black);
                float maxDimension = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
                if (maxDimension > 0f)
                {
                    model.transform.localScale = Vector3.one * (targetSize / maxDimension);
                }
                return model;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to build pickup model {name}: {e}");
                return fallback != null ? fallback() : MysteryModel();
            }
        }

        internal static GameObject MysteryModel()
        {
            return Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
        }

        // A chunky pixel-font glyph extruded from cubes ('X' = filled cell, top row first),
        // lightly emissive. Used as a model fallback when no generated OBJ is embedded.
        internal static GameObject CreateGlyphModel(string name, string[] glyph, Color color, float cellSize, float depth)
        {
            try
            {
                var model = new GameObject(name);
                model.transform.SetParent(PrefabHolder);
                model.AddComponent<MeshFilter>().mesh = BuildGlyphMesh(glyph, cellSize, depth);
                model.AddComponent<MeshRenderer>().material = CreatePickupMaterial(color, null, color * 0.4f);
                return model;
            }
            catch (Exception e)
            {
                Log.Warning($"Falling back to mystery pickup model for {name}: {e.Message}");
                return MysteryModel();
            }
        }

        private static Mesh BuildGlyphMesh(string[] glyph, float cellSize, float depth)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            int rows = glyph.Length;
            int cols = glyph[0].Length;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (glyph[r][c] != 'X')
                    {
                        continue;
                    }
                    var center = new Vector3(
                        (c + 0.5f - cols / 2f) * cellSize,
                        (rows - 1 - r + 0.5f - rows / 2f) * cellSize,
                        0f);
                    AddCube(vertices, normals, uvs, triangles, center, new Vector3(cellSize, cellSize, depth));
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCube(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles, Vector3 center, Vector3 size)
        {
            var half = size * 0.5f;
            // right, left, up, down, forward, back
            var faceNormals = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var normal in faceNormals)
            {
                // build a tangent basis for this face
                var u = normal == Vector3.up || normal == Vector3.down ? Vector3.right : Vector3.Cross(Vector3.up, normal);
                var v = Vector3.Cross(normal, u);
                var faceCenter = center + Vector3.Scale(normal, half);
                var uHalf = Vector3.Scale(u, half);
                var vHalf = Vector3.Scale(v, half);

                int baseIndex = vertices.Count;
                vertices.Add(faceCenter - uHalf - vHalf);
                vertices.Add(faceCenter - uHalf + vHalf);
                vertices.Add(faceCenter + uHalf + vHalf);
                vertices.Add(faceCenter + uHalf - vHalf);
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(normal);
                }
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                // clockwise from outside (Unity front-face winding)
                triangles.AddRange(new[] { baseIndex, baseIndex + 2, baseIndex + 1, baseIndex, baseIndex + 3, baseIndex + 2 });
            }
        }
    }
}
