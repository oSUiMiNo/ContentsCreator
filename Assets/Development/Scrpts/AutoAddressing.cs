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
        // 追加のみ処理
        if (importedAssets.Length <= 0) return;

        var settings = AddressableAssetSettingsDefaultObject.Settings;

        foreach (var asset in importedAssets)
        {
            // 対象のフォルダ以外のファイルは除外
            //if (!asset.Contains(TARGET_DIRECTORY)) continue;
            if (Path.GetDirectoryName(Path.GetDirectoryName(asset)) != @"Assets\Contents") continue;

            Debug.Log(Path.GetDirectoryName(asset).Replace(@$"{TARGET_DIRECTORY.Replace("/", "\\")}\", ""));

            // フォルダは除外
            if (File.GetAttributes(asset).HasFlag(FileAttributes.Directory)) continue;

            var guid = AssetDatabase.AssetPathToGUID(asset);
            var group = settings.DefaultGroup;
            var assetEntry = settings.CreateOrMoveEntry(guid, group);

            // Simplify addressable name
            assetEntry.SetAddress(Guid.NewGuid().ToString());

            switch (Path.GetDirectoryName(asset).Replace(@$"{TARGET_DIRECTORY.Replace("/", "\\")}\", ""))
            {
                case "Avatar":
                    assetEntry.SetLabel("アバター", true, true);
                    break;
                case "Funiture":
                    assetEntry.SetLabel("家具", true, true);
                    break;
                case "Room":
                    assetEntry.SetLabel("部屋", true, true);
                    break;
                case "Motion":
                    assetEntry.SetLabel("モーション", true, true);
                    break;
                case "Sky":
                    assetEntry.SetLabel("空", true, true);
                    break;
            }

        }
        AssetDatabase.SaveAssets();
    }
}
#endif