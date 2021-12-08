using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.VegetationSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class ConvertGoToVSPro : MonoBehaviour
{
    public List<GameObject> GameObjects = new List<GameObject>();

    public bool DeactivateGameObjects = true;
    public bool SkipDeactivatedGameObjects = true;

    private int _skiped = 0;
    private int _processed = 0;
    private string _latestStatus = null;

    public string Status => _latestStatus;

    public void Convert(PersistentVegetationStorage PersistentStorage, string VegetationItemID)
    {
        _skiped = 0;
        _processed = 0;
        _latestStatus = null;

        for (int i = 0; i< GameObjects.Count; i++)
        {
            GameObject gameObject = GameObjects[i];

            if (gameObject == null) continue;

            if (SkipDeactivatedGameObjects && !gameObject.activeSelf)
            {
                _skiped++;
                continue;
            }

            PersistentStorage.AddVegetationItemInstance(
                        vegetationItemID: VegetationItemID,
                        worldPosition: gameObject.transform.position,
                        scale: gameObject.transform.localScale,
                        rotation: gameObject.transform.rotation,
                        applyMeshRotation: true,    // ??
                        vegetationSourceID: 3,      // Scene Objects Importer
                        distanceFalloff: 1,
                        clearCellCache: true);

            _processed++;

            if (DeactivateGameObjects)
                gameObject.SetActive(false);
        }

        _latestStatus = $"Converted GameObjects : {_processed},\n Skipped GameObjects (unactive) : {_skiped}";
    }
}
