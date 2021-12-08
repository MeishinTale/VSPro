using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeTechnologies.Extensions;
using UnityEngine;
using UnityEditor;
using AwesomeTechnologies.Utility;
using AwesomeTechnologies.Utility.Culling;
using AwesomeTechnologies.Utility.Extentions;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.VegetationSystem;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies;

[CustomEditor(typeof(ConvertGoToVSPro))]
public class ConvertGoToVSProEditor : VegetationStudioProBaseEditor
{
    private ConvertGoToVSPro _convertGoToVSPro;
    private PersistentVegetationStorage _persistentVegetationStorage;
    SerializedProperty m_GameObjects;

    void OnEnable()
    {
        _convertGoToVSPro = (ConvertGoToVSPro)target;
        m_GameObjects = serializedObject.FindProperty("GameObjects");
    }

    void SetSceneDirty()
    {
        if (!Application.isPlaying && _persistentVegetationStorage != null)
        {
            EditorSceneManager.MarkSceneDirty(_persistentVegetationStorage.gameObject.scene);
            EditorUtility.SetDirty(_persistentVegetationStorage);
        }
    }

    public override void OnInspectorGUI()
    {
        //HelpTopic = "persistent-vegetation-storage";
        //OverrideLogoTextureName = "Banner_PersistentVegetationStorage";
        //LargeLogo = false;

        _convertGoToVSPro = (ConvertGoToVSPro)target;
        _persistentVegetationStorage = _convertGoToVSPro.GetComponent<PersistentVegetationStorage>();
        ShowLogo = false;

        base.OnInspectorGUI();

        if (!_persistentVegetationStorage)
        {
            EditorGUILayout.HelpBox(
                "The Converter requires to be on the same GqmeObject as the PersistentVegetationStorage Component.",
                MessageType.Error);
            return;
        }

        if (!_persistentVegetationStorage.VegetationSystemPro)
        {
            EditorGUILayout.HelpBox(
                "The PersistentVegetationStorage Component needs to be added to a GameObject with a VegetationSystemPro Component.",
                MessageType.Error);
            return;
        }

        if (!_persistentVegetationStorage.VegetationSystemPro.InitDone)
        {
            GUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox(
                "Vegetation system component has configuration errors. Fix to enable component.",
                MessageType.Error);
            GUILayout.EndVertical();
            return;
        }

        DrawVegetationPickerInspector();

        DrawVegetationConverter();
    }

    /// <summary>
    /// Note : We're re-using PersistentVegetationStorage.SelectedPaintVegetationID
    /// </summary>
    private void DrawVegetationPickerInspector()
    {
        if (!IsPersistentoragePackagePresent()) return;

        SelectVegetationPackage();

        if (_persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList.Count == 0) return;

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Select Vegetation Item", LabelStyle);

        VegetationPackagePro vegetationPackagePro =
            _persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList[
                _persistentVegetationStorage.SelectedVegetationPackageIndex];

        VegetationPackageEditorTools.DrawVegetationItemSelector(
            vegetationPackagePro,
            VegetationPackageEditorTools.CreateVegetationInfoIdList(
                vegetationPackagePro,
                new[]
                {
                        VegetationType.Grass, VegetationType.Plant, VegetationType.Tree, VegetationType.Objects,
                        VegetationType.LargeObjects
                }), 60,
            ref _persistentVegetationStorage.SelectedPaintVegetationID);

        GUILayout.EndVertical();
    }

    private void DrawVegetationConverter()
    {
        GUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();
        _convertGoToVSPro.DeactivateGameObjects = EditorGUILayout.Toggle(new GUIContent("Deactivate on process", "Check to deactivate GameObjects processed automatically"), _convertGoToVSPro.DeactivateGameObjects) ;

        _convertGoToVSPro.SkipDeactivatedGameObjects = EditorGUILayout.Toggle(new GUIContent("Skip de-activated", "Check to skip deactivated GameObjects from being processed"), _convertGoToVSPro.SkipDeactivatedGameObjects);

        GUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(m_GameObjects, new GUIContent("Scene Game Objects"));
        
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        if (_persistentVegetationStorage != null && 
            !string.IsNullOrEmpty(_persistentVegetationStorage.SelectedPaintVegetationID) &&
            _convertGoToVSPro.GameObjects.Count != 0)
        {
            if (GUILayout.Button("Convert GameObjects"))
            {
                _convertGoToVSPro.Convert(_persistentVegetationStorage, _persistentVegetationStorage.SelectedPaintVegetationID);
                SetSceneDirty();
            }

            if (!string.IsNullOrEmpty(_convertGoToVSPro.Status))
                EditorGUILayout.HelpBox(_convertGoToVSPro.Status, MessageType.Info);
        }

    }

    bool IsPersistentoragePackagePresent()
    {
        if (!_persistentVegetationStorage.PersistentVegetationStoragePackage)
        {
            EditorGUILayout.HelpBox("You need to add a persistent vegetation package to the component.",
                MessageType.Error);
            return false;
        }

        if (_persistentVegetationStorage.PersistentVegetationStoragePackage.PersistentVegetationCellList
                .Count != _persistentVegetationStorage.VegetationSystemPro.VegetationCellList.Count)
        {
            EditorGUILayout.HelpBox("The vegetation storage is not initialized or initialized for another world or cell size.",
                MessageType.Error);
            return false;
        }

        return true;
    }

    void SelectVegetationPackage()
    {
        if (_persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList.Count == 0)
        {
            EditorGUILayout.HelpBox("The vegetation system does not have any biomes/vegetation packages",
                MessageType.Warning);
            return;
        }

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Select biome/vegetation package", LabelStyle);
        string[] packageNameList =
            new string[_persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList.Count];
        for (int i = 0;
            i <= _persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList.Count - 1;
            i++)
        {
            if (_persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList[i])
            {
                packageNameList[i] = (i + 1).ToString() + " " + _persistentVegetationStorage.VegetationSystemPro
                                         .VegetationPackageProList[i].PackageName + " (" + _persistentVegetationStorage.VegetationSystemPro.VegetationPackageProList[i].BiomeType.ToString() + ")"; ;
            }
            else
            {
                packageNameList[i] = "Not found";
            }
        }

        EditorGUI.BeginChangeCheck();
        _persistentVegetationStorage.SelectedVegetationPackageIndex = EditorGUILayout.Popup(
            "Selected vegetation package", _persistentVegetationStorage.SelectedVegetationPackageIndex,
            packageNameList);
        if (EditorGUI.EndChangeCheck())
        {
        }

        GUILayout.EndVertical();
    }
}
