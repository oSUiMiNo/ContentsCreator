using NUnit.Framework.Interfaces;
using System.IO;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class AutoAddressing : AssetPostprocessor
{

    private const string TARGET_DIRECTORY = "Assets/Contents";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        // �ǉ��̂ݏ���
        if (importedAssets.Length <= 0) return;

        var settings = AddressableAssetSettingsDefaultObject.Settings;

        foreach (var asset in importedAssets)
        {
            // �Ώۂ̃t�H���_�ȊO�̃t�@�C���͏��O
            //if (!asset.Contains(TARGET_DIRECTORY)) continue;
            if (Path.GetDirectoryName(Path.GetDirectoryName(asset)) != @"Assets\Contents") continue;

            Debug.Log(Path.GetDirectoryName(asset).Replace(@$"{TARGET_DIRECTORY.Replace("/", "\\")}\", ""));

            // �t�H���_�͏��O
            if (File.GetAttributes(asset).HasFlag(FileAttributes.Directory)) continue;

            var guid = AssetDatabase.AssetPathToGUID(asset);
            var group = settings.DefaultGroup;
            var assetEntry = settings.CreateOrMoveEntry(guid, group);

            // Simplify addressable name
            assetEntry.SetAddress(Guid.NewGuid().ToString());

            switch (Path.GetDirectoryName(asset).Replace(@$"{TARGET_DIRECTORY.Replace("/", "\\")}\", ""))
            {
                case "Avatar":
                    assetEntry.SetLabel("�A�o�^�[", true, true);
                    break;
                case "Funiture":
                    assetEntry.SetLabel("�Ƌ�", true, true);
                    break;
                case "Room":
                    assetEntry.SetLabel("����", true, true);
                    break;
                case "Motion":
                    assetEntry.SetLabel("���[�V����", true, true);
                    break;
                case "Sky":
                    assetEntry.SetLabel("��", true, true);
                    break;
            }

        }
        AssetDatabase.SaveAssets();
    }
}
#endif