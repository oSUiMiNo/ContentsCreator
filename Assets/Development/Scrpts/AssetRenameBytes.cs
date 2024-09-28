using UnityEngine;
using System.IO;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;

public class AssetRenameBytes : AssetPostprocessor
{
    /// <summary>
    /// あらゆる種類の任意の数のアセットがインポートが完了したときに呼ばれる処理
    /// </summary>
    /// <param name="importedAssets"> インポートされたアセットのファイルパス。 </param>
    /// <param name="deletedAssets"> 削除されたアセットのファイルパス。 </param>
    /// <param name="movedAssets"> 移動されたアセットのファイルパス。 </param>
    /// <param name="movedFromPath"> 移動されたアセットの移動前のファイルパス。 </param>
    static void OnPostprocessAllAssets
        (string[] importedAssets, string[] deletedAssets,
         string[] movedAssets, string[] movedFromPath)
    {
        // アセットがインポートされた場合
        foreach (string asset in importedAssets)
        {
            // 拡張子のみ取得
            string type = Path.GetExtension(asset);

            // vrmファイルをインポートした時
            if (type == ".vrm")
            {
                VRM_To_TextAsset(asset);
            }

            //// bytesファイル作成時に走る処理
            //if (type == ".bytes")
            //{
            //    // 拡張子なしのファイル名を取得
            //    string fileNm = Path.GetFileNameWithoutExtension(asset);
            //    // コピー元ファイルとmetaデータを削除
            //    FileUtil.DeleteFileOrDirectory(asset.Replace(".bytes", ""));
            //    FileUtil.DeleteFileOrDirectory(asset.Replace(".bytes", ".meta"));
            //    Debug.Log(fileNm + "をbytes拡張子に変換");
            //    // Editorに反映されるの遅いのでリフレッシュ
            //    AssetDatabase.Refresh();
            //}
        }
    }

    public static void VRM_To_TextAsset(string path)
    {
        string name = Path.GetFileName(path).Replace(Path.GetExtension(path), "");
        byte[] vrmAsBytes = File.ReadAllBytes(path);
        //string vrmAsTxt = Convert.ToBase64String(vrmAsBytes);
        //TextAsset vrmTextAsset = new TextAsset(Convert.ToBase64String(vrmAsBytes));

        //string savePath_VRMAsTxt = @$"Assets/VRM_As_TextAsset/{name}.txt";
        string savePath_VRMAsBytes = @$"Assets/VRMAsBytes/{name}.bytes";

        try
        {
            //File.WriteAllText(savePath_VRMAsTxt, vrmAsTxt);
            File.WriteAllBytes(savePath_VRMAsBytes, vrmAsBytes);
            Debug.Log("VRMファイルが正常に作成されました: " + savePath_VRMAsBytes);
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.Log("エラーが発生しました: " + ex.Message);
        }
    }
}
#endif
