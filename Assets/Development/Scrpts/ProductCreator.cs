using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEditor.Build.Reporting;


public class Shiji
{
    public Name_Content Hash { get; set; }
    public Name_Content Catalog { get; set; }
    public string Addresses { get; set; }
    public List<Name_Content> Bundles { get; set; }
}
public class Name_Content
{
    public string FileName { get; set; }
    public string Content { get; set; }
}





[CustomEditor(typeof(ProductCreator))]//拡張するクラスを指定
public class ExampleScriptEditor : Editor
{
    // InspectorのGUIを更新
    public override void OnInspectorGUI()
    {
        //元のInspector部分を表示
        base.OnInspectorGUI();

        //targetを変換して対象を取得
        ProductCreator creator = target as ProductCreator;

        // ボタンのスタイルを定義
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 48; // ボタンの文字サイズを設定
        buttonStyle.alignment = TextAnchor.MiddleCenter; // ボタンの文字を真ん中に
        buttonStyle.fontStyle = FontStyle.Bold; // 文字を太く設定

        // ボタンの背景色を青に設定
        Color originalColor = GUI.backgroundColor;

        // 縦並びにする
        GUILayout.BeginVertical();

        // ここで横並びかつフレキシブルスペースを追加してボタン全体を右寄せ
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // ボタンの背景色を黄色に設定して最初のボタン
        //GUI.backgroundColor = Color.yellow;
        //if (GUILayout.Button("Build", buttonStyle, GUILayout.Width(190), GUILayout.Height(80)))
        //{
        //    creator.BuildAddressableContent();
        //}

        GUILayout.EndHorizontal(); // 最初のボタンの右寄せ処理終了

        // もう一つのボタンも同様に右寄せ処理
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // ボタンの背景色を青に設定して次のボタン
        GUI.backgroundColor = Color.blue;
        if (GUILayout.Button("Export", buttonStyle, GUILayout.Width(190), GUILayout.Height(80)))
        {
            //creator.ExportFiles();
            creator.ExportShiji();
        }

        GUILayout.EndHorizontal(); // 2つ目のボタンの右寄せ処理終了

        // 縦並び終了
        GUILayout.EndVertical();

        // 元の色に戻す
        GUI.backgroundColor = originalColor;
    }
}






[CreateAssetMenu(fileName = "ProductCreator", menuName = "Scriptable Objects/ProductCreator")]
public class ProductCreator : ScriptableObject
{
    [SerializeField] string フォルダ名;

    private const string NotionAccessToken = "secret_OIxSWO69mxnD9FNbmL2US0pcsLWCUmsaglBZBCWPWrC"; //新しい方

    string path;

    List<string> labels = new List<string>
    {
        "部屋",
        "アバター",
        "モーション",
        "空",
        "家具"
    };

   
    // エクスポート
    public async void ExportShiji()
    {
        ClearConsole();
        AssetDatabase.Refresh();

        if (string.IsNullOrEmpty(フォルダ名))
        {
            Debug.LogAssertion("ファイル名を決めてください");
            return;
        }

        path = @$"{Application.dataPath.Replace("Assets", "")}{Addressables.BuildPath}/StandaloneWindows64";
        //Debug.Log(GetOnlyOneFile(path, "json"));

        Debug.Log("ビルド開始");
        BuildAddressableContent();


        Debug.Log("ビルド完了");

        Debug.Log("エクスポート開始");

        if (!Directory.Exists(path)) return;
        Debug.Log($"パス{path}");
       
        Directory.CreateDirectory(@$"{Application.dataPath}/Publish/{フォルダ名}");
        await Delay.Frame(1);
        AssetDatabase.Refresh();

        Debug.Log($"パーシステント{Application.persistentDataPath}");

        string addresses = string.Empty;
        // リソースロケータ（取得したカタログがデシリアライズされたもの）を取得
        IResourceLocator resourceLocator = await GetLocator(GetOnlyOneFile(path, "json"));
        // カタログ(メモリ上ではIResourceLocatorとして扱われる)からアドレス一覧を抽出
        // IResourceLocator は key(通常はAdress)と、それに紐づくアセットの対応関係の情報を持っている
        //foreach (var address in resourceLocator.Keys)
        //{
        //    Debug.Log(address);
        //    allAddresses += address;
        //}
        Debug.Log($"0");

        foreach (var label in labels)
        {
            Debug.Log($"1");

            if (label == "部屋" || label == "家具")
            {
                foreach (var address in await ExtractContentAdresses_Go(resourceLocator, label))
                {
                    Debug.Log(address);
                    addresses += $"{address}|||";
                }
            }
            if (label == "アバター")
            {
                foreach (var address in await ExtractContentAdresses_Txt(resourceLocator, label))
                {
                    Debug.Log(address);
                    addresses += $"{address}|||";
                }
            }
            if (label == "空")
            {
                foreach (var address in await ExtractContentAdresses_Sky(resourceLocator, label))
                {
                    Debug.Log(address);
                    addresses += $"{address}|||";
                }
            }
        }

        Debug.Log($"2");

        Name_Content hash = new Name_Content()
        {
            FileName = System.IO.Path.GetFileName(Directory.GetFiles(path, $"*.hash")[0]),
            Content = File.ReadAllText(Directory.GetFiles(path, $"*.hash")[0])
        };


        Name_Content catalog = new Name_Content()
        {
            FileName = System.IO.Path.GetFileName(Directory.GetFiles(path, $"*.json")[0]),
            Content = File.ReadAllText(Directory.GetFiles(path, $"*.json")[0])
        };


        // .bundleファイルの中身を文字列として読み込む
        List<Name_Content> bundles = new List<Name_Content>();
        foreach (string bundle in Directory.GetFiles(path, $"*.bundle"))
        {
            // バイナリファイルをバイト配列として読み込む
            byte[] bundleBytes = File.ReadAllBytes(bundle);
            // バイト配列をBase64文字列に変換して追加する（バイナリファイルのまま扱いたい場合）
            string bundleString = Convert.ToBase64String(bundleBytes);

            Name_Content bundleAsTxt = new Name_Content()
            {
                FileName = System.IO.Path.GetFileName(bundle),
                Content = bundleString
            };

            bundles.Add(bundleAsTxt);
        }
        Debug.Log($"3");

        // シリアライズするデータを作成
        Shiji shiji = new Shiji
        {
            Hash = hash,
            Catalog = catalog,
            Addresses = addresses,
            Bundles = bundles
        };


        // シリアライズ
        string jsonString = JsonConvert.SerializeObject(shiji, Formatting.Indented);
        Debug.Log(jsonString);

        //// バイト配列に変換
        //byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);


        string shijiPath = @$"{Application.dataPath}/Publish/{フォルダ名}/{hash.Content}.shiji";
        Debug.Log($"4");

        // ファイルに書き込む
        File.WriteAllText(shijiPath, jsonString);

        //// .bytes ファイルを .shizi ファイルとしてコピー
        //File.Copy(bytesPath, shiziPath, true); // 上書き許可

        AssetDatabase.Refresh();
        Debug.Log("エクスポート完了");
    }


    //public async void ExportFiles()
    //{
    //    if (string.IsNullOrEmpty(フォルダ名))
    //    {
    //        Debug.LogAssertion("フォルダ名を決めてください");
    //        return;
    //    }

    //    string path = @$"{Application.dataPath.Replace("Assets", "")}{Addressables.BuildPath}/StandaloneWindows64";
    //    string newPath = @$"{Application.dataPath}/Publish/{フォルダ名}";
    //    if (!Directory.Exists(path)) return;
    //    Debug.Log($"パス{path}");

    //    Debug.Log($"パーシステント{Application.persistentDataPath}");

    //    string allAddresses = string.Empty;
    //    // リソースロケータ（取得したカタログがデシリアライズされたもの）を取得
    //    IResourceLocator resourceLocator = await GetLocator(GetOnlyOneFile(path, "json"));
    //    // カタログ(メモリ上ではIResourceLocatorとして扱われる)からアドレス一覧を抽出
    //    // IResourceLocator は key(通常はAdress)と、それに紐づくアセットの対応関係の情報を持っている
    //    //foreach (var address in resourceLocator.Keys)
    //    //{
    //    //    Debug.Log(address);
    //    //    allAddresses += address;
    //    //}
    //    foreach (var label in labels)
    //    {
    //        if (label == "部屋" || label == "家具")
    //        {
    //            foreach (var address in await ExtractContentAdresses_Go(resourceLocator, label))
    //            {
    //                Debug.Log(address);
    //                allAddresses += $"{address}|||";
    //            }
    //        }
    //        if (label == "アバター")
    //        {
    //            foreach (var address in await ExtractContentAdresses_Txt(resourceLocator, label))
    //            {
    //                Debug.Log(address);
    //                allAddresses += $"{address}|||";
    //            }
    //        }
    //        if (label == "空")
    //        {
    //            foreach (var address in await ExtractContentAdresses_Sky(resourceLocator, label))
    //            {
    //                Debug.Log(address);
    //                allAddresses += $"{address}|||";
    //            }
    //        }
    //    }
    //    Debug.Log(allAddresses);


    //    // カタログ等が入っているディレクトリの中身を自プロジェクトのフォルダ内に複製
    //    DirectoryUtil.CopyParentFolder(path, newPath);

    //    foreach (string bundle in Directory.GetFiles(newPath, $"*.bundle"))
    //    {
    //        // .bundleファイル名を取得し、拡張子を .bytes に変更
    //        string bytesFilePath = Path.ChangeExtension(bundle, ".bytes");
    //        // .bundle ファイルを .bytes ファイルとしてコピー
    //        File.Copy(bundle, bytesFilePath, true); // 上書き許可
    //        // 元の .bundle ファイルを削除
    //        File.Delete(bundle);
    //    }


    //    try
    //    {
    //        // ファイルに文字列を書き込む
    //        File.WriteAllText(@$"{newPath}/Addresses.txt", allAddresses);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogAssertion("エラーが発生しました: " + ex.Message);
    //    }
    //    try
    //    {
    //        // ファイルに文字列を書き込む
    //        File.WriteAllText(@$"{newPath}/Hash.txt", File.ReadAllText(Directory.GetFiles(newPath, $"*.hash")[0]));
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogAssertion("エラーが発生しました: " + ex.Message);
    //    }
    //    try
    //    {
    //        // ファイルに文字列を書き込む
    //        File.WriteAllText(@$"{newPath}/ID.txt", Guid.NewGuid().ToString());
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogAssertion("エラーが発生しました: " + ex.Message);
    //    }

    //    AssetDatabase.Refresh();
    //}


    // Addressableのビルド処理



    [MenuItem("Tools/Clear Console %#c")]
    static void ClearConsole()
    {
        //var logEntries = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
        //var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        //clearMethod.Invoke(null, null);

        // Unityエディター上でコンソールをクリア
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }


    public async void BuildAddressableContent()
    {
        Debug.Log("Addressableビルドを開始します...");
        // Addressablesのキャッシュをクリア
        AddressableAssetSettings.CleanPlayerContent();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetSettings.BuildPlayerContent();
        // カタログをロードする（例）
        AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(GetOnlyOneFile(path, "json"));
        await handle;

        // ここで handle を使った処理を行う
        // 処理が完了したらリリース
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError("Failed to load content catalog.");
        }
        Debug.Log("Addressableビルドが完了しました。");
    }


    protected async UniTask<string> Duplicate(string orig, string dest)
    {
        Directory.CreateDirectory(dest);
        // カタログ等が入っているディレクトリの中身を自プロジェクトのフォルダ内に複製
        DirectoryUtil.CopyParentFolder(orig, dest);

        string newPath = $@"{dest}/{Path.GetFileName(orig)}";
        return newPath;
    }


    string GetOnlyOneFile(string dir, string extention)
    {
        // 指定した拡張子のファイル一覧を取得
        string[] files = Directory.GetFiles(dir, $"*.{extention}");
        // 該当ファイルが１つの場合のみ返す
        if (files.Length == 1)
        {
            return files[0];
        }
        else
        if (files.Length == 0)
        {
            Debug.LogAssertion($"指定したフォルダ {dir} に「.{extention}」ファイルは無い");
            return null;
        }
        else
        {
            Debug.LogAssertion($"指定したフォルダ {dir} に「.{extention}」ファイルが複数ある");
            return null;
        }
    }

   

    // 使いたいカタログをリソースロケータに登録
    public async UniTask<IResourceLocator> GetLocator(string catalogPath)
    {
        // 新しいカタログを取得。ファイルパスかURL
        AsyncOperationHandle<IResourceLocator> requestCatalog
            = Addressables.LoadContentCatalogAsync(catalogPath);

        // ロード完了を待つ
        await requestCatalog;

        // 何のエラー処理だろ
        Assert.AreEqual(AsyncOperationStatus.Succeeded, requestCatalog.Status);

        // リソースロケータ（取得したカタログがデシリアライズされたもの）を返す
        return requestCatalog.Result;
    }


    // 該当コンテンツのアドレス一覧を取得
    public async UniTask<List<string>> ExtractContentAdresses_Go(IResourceLocator resourceLocator, string label)
    {
        Addressables.AddResourceLocator(resourceLocator);

        List<string> addresses = new List<string>();
        List<string> loadedAddresses = new List<string>();

        AsyncOperationHandle<IList<GameObject>> handle_LabelGobj = new AsyncOperationHandle<IList<GameObject>>();
        try
        {
            handle_LabelGobj = Addressables.LoadAssetsAsync<GameObject>(label, null);
            await handle_LabelGobj;
        }
        catch (Exception e)
        {
            Debug.Log($"このカタログにラベル {label} は無い");
            return new List<string>();
        }


        // カタログ(メモリ上ではIResourceLocatorとして扱われる)からアドレス一覧を抽出
        // IResourceLocator は key(通常はAdress)と、それに紐づくアセットの対応関係の情報を持っている
        foreach (var a in resourceLocator.Keys)
        {
            addresses.Add(a.ToString());
        }

        // ロードできるものを判別して、今回ロードしたいタイプのアセットか判別して、全部ロード
        foreach (var a in addresses)
        {
            Debug.Log($"------ アドレス : {a}");
            IList<IResourceLocation> resourceLocations;
            // IResourceLocation はアセットをロードするために必要な情報を持っている
            // IResourceLocator の Locate() に特定のアドレスを渡すと IResourceLocation が返ってくる
            // つまりカタログが IResourceLocation を内包している
            if (!resourceLocator.Locate(a, typeof(GameObject), out resourceLocations)) continue;


            foreach (var b in resourceLocations)
            {
                Debug.Log($"------- プライマリーキー {b.PrimaryKey}");
                // IResourceLocation の PrimaryKey がアドレスらしい

                AsyncOperationHandle<GameObject> opHandle_LoadedGObj = new AsyncOperationHandle<GameObject>();
                try
                {
                    opHandle_LoadedGObj = Addressables.LoadAssetAsync<GameObject>(b.PrimaryKey);
                    await opHandle_LoadedGObj;
                }
                catch (Exception e)
                {
                    //Debug.Log($"{b.PrimaryKey} はロードできなかった");
                    continue;
                }

                // 今回のラベルに含まれているゲームオブジェクトかどうか
                bool containedInLabel = false;
                foreach (GameObject labelGobj in handle_LabelGobj.Result)
                {
                    if (opHandle_LoadedGObj.Result == labelGobj)
                    {
                        Debug.Log($"ラベルに含まれていた {opHandle_LoadedGObj.Result}");
                        containedInLabel = true;
                        continue;
                    }
                }
                if (containedInLabel == false) continue;

                // ロードできなかったら次のループへ
                if (opHandle_LoadedGObj.Status != AsyncOperationStatus.Succeeded) continue;
                //Debug.Log($"------ ロードしたゲームオブジェクト : {opHandle_LoadedGObj.Result}");

                // シーン内の物体としてのゲームオブジェクト(つまりRendererコンポーネントがついているはず)かどうかを判別して、
                // 違ったら次のループへ
                Renderer[] renderers = opHandle_LoadedGObj.Result.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                //Debug.Log($"------ レンダラを持つゲームオブジェクト : {opHandle_LoadedGObj.Result}");

                // 既にロード済みのアドレスに含まれていたら次のループへ
                if (loadedAddresses.Contains(b.PrimaryKey)) continue;
                // 今回のアドレスを、ロード済みのアドレス一覧loadedAddressesに記録
                loadedAddresses.Add(b.PrimaryKey);
            }
        }

        Addressables.RemoveResourceLocator(resourceLocator);
        return loadedAddresses;
    }

    // 該当コンテンツのアドレス一覧を取得
    public async UniTask<List<string>> ExtractContentAdresses_Txt(IResourceLocator resourceLocator, string label)
    {
        Addressables.AddResourceLocator(resourceLocator);

        List<string> addresses = new List<string>();
        List<string> loadedAddresses = new List<string>();

        AsyncOperationHandle<IList<TextAsset>> handle_LabelGobj = new AsyncOperationHandle<IList<TextAsset>>();
        try
        {
            handle_LabelGobj = Addressables.LoadAssetsAsync<TextAsset>(label, null);
            await handle_LabelGobj;
        }
        catch (Exception e)
        {
            Debug.Log($"このカタログにラベル {label} は無い");
            return new List<string>();
        }


        // カタログ(メモリ上ではIResourceLocatorとして扱われる)からアドレス一覧を抽出
        // IResourceLocator は key(通常はAdress)と、それに紐づくアセットの対応関係の情報を持っている
        foreach (var a in resourceLocator.Keys)
        {
            addresses.Add(a.ToString());
        }

        // ロードできるものを判別して、今回ロードしたいタイプのアセットか判別して、全部ロード
        foreach (var a in addresses)
        {
            Debug.Log($"------ アドレス : {a}");
            IList<IResourceLocation> resourceLocations;
            // IResourceLocation はアセットをロードするために必要な情報を持っている
            // IResourceLocator の Locate() に特定のアドレスを渡すと IResourceLocation が返ってくる
            // つまりカタログが IResourceLocation を内包している
            if (!resourceLocator.Locate(a, typeof(TextAsset), out resourceLocations)) continue;


            foreach (var b in resourceLocations)
            {
                Debug.Log($"------- プライマリーキー {b.PrimaryKey}");
                // IResourceLocation の PrimaryKey がアドレスらしい

                AsyncOperationHandle<TextAsset> opHandle_LoadedGObj = new AsyncOperationHandle<TextAsset>();
                try
                {
                    opHandle_LoadedGObj = Addressables.LoadAssetAsync<TextAsset>(b.PrimaryKey);
                    await opHandle_LoadedGObj;
                }
                catch (Exception e)
                {
                    Debug.Log($"{b.PrimaryKey} はロードできなかった");
                    continue;
                }

                // 今回のラベルに含まれているゲームオブジェクトかどうか
                bool containedInLabel = false;
                foreach (TextAsset labelGobj in handle_LabelGobj.Result)
                {
                    if (opHandle_LoadedGObj.Result == labelGobj)
                    {
                        Debug.Log($"ラベルに含まれていた {opHandle_LoadedGObj.Result}");
                        containedInLabel = true;
                        continue;
                    }
                }
                if (containedInLabel == false) continue;

                // ロードできなかったら次のループへ
                if (opHandle_LoadedGObj.Status != AsyncOperationStatus.Succeeded) continue;
                //Debug.Log($"------ ロードしたゲームオブジェクト : {opHandle_LoadedGObj.Result}");

                // 既にロード済みのアドレスに含まれていたら次のループへ
                if (loadedAddresses.Contains(b.PrimaryKey)) continue;
                // 今回のアドレスを、ロード済みのアドレス一覧loadedAddressesに記録
                loadedAddresses.Add(b.PrimaryKey);
            }
        }

        Addressables.RemoveResourceLocator(resourceLocator);
        return loadedAddresses;
    }

    // 該当コンテンツのアドレス一覧を取得
    public async UniTask<List<string>> ExtractContentAdresses_Sky(IResourceLocator resourceLocator, string label)
    {
        Addressables.AddResourceLocator(resourceLocator);

        List<string> addresses = new List<string>();
        List<string> loadedAddresses = new List<string>();

        AsyncOperationHandle<IList<Material>> handle_LabelGobj = new AsyncOperationHandle<IList<Material>>();
        try
        {
            handle_LabelGobj = Addressables.LoadAssetsAsync<Material>(label, null);
            await handle_LabelGobj;
        }
        catch (Exception e)
        {
            Debug.Log($"このカタログにラベル {label} は無い");
            return new List<string>();
        }


        // カタログ(メモリ上ではIResourceLocatorとして扱われる)からアドレス一覧を抽出
        // IResourceLocator は key(通常はAdress)と、それに紐づくアセットの対応関係の情報を持っている
        foreach (var a in resourceLocator.Keys)
        {
            addresses.Add(a.ToString());
        }

        // ロードできるものを判別して、今回ロードしたいタイプのアセットか判別して、全部ロード
        foreach (var a in addresses)
        {
            Debug.Log($"------ アドレス : {a}");
            IList<IResourceLocation> resourceLocations;
            // IResourceLocation はアセットをロードするために必要な情報を持っている
            // IResourceLocator の Locate() に特定のアドレスを渡すと IResourceLocation が返ってくる
            // つまりカタログが IResourceLocation を内包している
            if (!resourceLocator.Locate(a, typeof(Material), out resourceLocations)) continue;


            foreach (var b in resourceLocations)
            {
                Debug.Log($"------- プライマリーキー {b.PrimaryKey}");
                // IResourceLocation の PrimaryKey がアドレスらしい

                AsyncOperationHandle<Material> opHandle_LoadedGObj = new AsyncOperationHandle<Material>();
                try
                {
                    opHandle_LoadedGObj = Addressables.LoadAssetAsync<Material>(b.PrimaryKey);
                    await opHandle_LoadedGObj;
                }
                catch (Exception e)
                {
                    Debug.Log($"{b.PrimaryKey} はロードできなかった");
                    continue;
                }

                // 今回のラベルに含まれているゲームオブジェクトかどうか
                bool containedInLabel = false;
                foreach (Material labelGobj in handle_LabelGobj.Result)
                {
                    if (opHandle_LoadedGObj.Result == labelGobj)
                    {
                        Debug.Log($"ラベルに含まれていた {opHandle_LoadedGObj.Result}");
                        containedInLabel = true;
                        continue;
                    }
                }
                if (containedInLabel == false) continue;

                // ロードできなかったら次のループへ
                if (opHandle_LoadedGObj.Status != AsyncOperationStatus.Succeeded) continue;
                //Debug.Log($"------ ロードしたゲームオブジェクト : {opHandle_LoadedGObj.Result}");

                // 既にロード済みのアドレスに含まれていたら次のループへ
                if (loadedAddresses.Contains(b.PrimaryKey)) continue;
                // 今回のアドレスを、ロード済みのアドレス一覧loadedAddressesに記録
                loadedAddresses.Add(b.PrimaryKey);
            }
        }

        Addressables.RemoveResourceLocator(resourceLocator);
        return loadedAddresses;
    }
}
#endif