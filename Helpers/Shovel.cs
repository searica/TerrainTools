using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using TerrainTools.Visualization;

namespace TerrainTools.Helpers
{
    internal class Shovel
    {
        private static bool HasBeenCreated = false;
        internal const string ShovelPrefabName = "ATM_Shovel";
        internal const string ShovelPieceTable = "_ShovelPieceTable";
        internal static CustomItem shovel;

        internal static void CreateShovel()
        {
            if (HasBeenCreated) return;
            var shovelPrefab = CreateShovelPrefab();

            CreatePieceTable();

            var reqs = new List<RequirementConfig>()
            {
                new RequirementConfig(item: "Wood", amount: 5, amountPerLevel: 1),
                new RequirementConfig(item: "Iron", amount: 2, amountPerLevel: 1)
            };

            var shovelConfig = new ItemConfig
            {
                Name = "Shovel",
                Description = "",
                CraftingStation = CraftingStations.Forge,
                PieceTable = ShovelPieceTable,
                RepairStation = CraftingStations.Forge,
                Requirements = reqs.ToArray(),
            };

            shovel = new CustomItem(shovelPrefab, true, shovelConfig);
            ItemManager.Instance.AddItem(shovel);
            HasBeenCreated = true;
        }

        private static void CreatePieceTable()
        {
            var pieceTableConfig = new PieceTableConfig()
            {
                CanRemovePieces = false,
                UseCategories = true,
                UseCustomCategories = false
            };
            var CPT = new CustomPieceTable(ShovelPieceTable, pieceTableConfig);
            PieceManager.Instance.AddPieceTable(CPT);
        }

        private static GameObject CreateShovelPrefab()
        {
            var shovel = PrefabManager.Instance.CreateClonedPrefab(ShovelPrefabName, "Hoe");

            // Get all the child objects I want to alter
            var handleCollider = shovel?.transform?.Find("collider")?.gameObject;
            var bladeCollider = shovel?.transform?.Find("collider (1)")?.gameObject;
            var attach = shovel?.transform?.Find("attach")?.gameObject;
            var visual = attach?.transform?.Find("visual")?.gameObject;

            if (shovel == null || handleCollider == null || bladeCollider == null || attach == null || visual == null)
            {
                Log.LogWarning("Failed to create shovel");
                return null;
            }

            var visualPos = attach.transform.localPosition + visual.transform.localPosition;
            var visualRot = attach.transform.localRotation * visual.transform.localRotation;
            // turn blade into iron shovel head
            try
            {
                var blade = visual?.transform?.Find("blade")?.gameObject;
                blade.transform.localPosition = new Vector3(0.0f, -0.01f, 0.815f);
                blade.transform.localRotation = Quaternion.identity;

                //blade.transform.localScale = new Vector3(0.275f, 0.08f, 0.6f);
                blade.transform.localScale = new Vector3(0.225f, 0.08f, 0.6f);

                // change material
                var bladeRender = blade.GetComponent<MeshRenderer>();
                bladeRender.sharedMaterial = PrefabManager.Cache.GetPrefab<Material>("iron");

                // adjust bladeCollider to match
                bladeCollider.transform.localPosition = visualPos + blade.transform.localPosition;
                bladeCollider.transform.localRotation = visualRot * blade.transform.localRotation;
                bladeCollider.transform.localScale = blade.transform.localScale;

                var boxCollider1 = bladeCollider.GetComponent<BoxCollider>();
                boxCollider1.center = Vector3.zero;
                boxCollider1.size = Vector3.one * 1.1f;
            }
            catch
            {
                Log.LogWarning("Failed to modify shovel blade");
            }

            try
            {
                // change handle to that of a shovel
                var handle = visual?.transform?.Find("handle")?.gameObject;
                var scale = handle.transform.localScale;
                scale.z = 1.6f;
                handle.transform.localScale = scale;

                // adjust handleCollider to match
                handleCollider.transform.localPosition = visualPos + handle.transform.localPosition;
                handleCollider.transform.localRotation = visualRot * handle.transform.localRotation;
                handleCollider.transform.localScale = handle.transform.localScale;

                var boxCollider = handleCollider.GetComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = new Vector3(1.5f, 1.5f, 1.025f);
            }
            catch
            {
                Log.LogWarning("Failed to modify shovel handle");
            }

            try
            {
                if (shovel.TryGetComponent(out ItemDrop itemDrop))
                {
                    var sharedData = itemDrop?.m_itemData?.m_shared;
                    if (sharedData != null)
                    {
                        var icon = IconCache.Shovel;
                        var sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.zero);
                        sharedData.m_icons = new Sprite[] { sprite };
                    }
                }
            }
            catch
            {
                Log.LogWarning("Failed to create shovel icon");
            }

            return shovel;
        }
    }
}