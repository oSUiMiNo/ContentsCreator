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


// .bundleファイルが長すぎてアップできない
public class Uploader : MonoBehaviour
{
    private const string NotionAccessToken = "secret_OIxSWO69mxnD9FNbmL2US0pcsLWCUmsaglBZBCWPWrC"; //新しい方

    public List<string> labels = new List<string>
    {
        "部屋",
        "アバター",
        "モーション",
        "空",
        "家具"
    };



    private async void Start()
    {
        string path = @$"{Application.dataPath.Replace("Assets", "")}{Addressables.BuildPath}/StandaloneWindows64";
        Debug.Log($"パス{path}");

        if (!Directory.Exists(path)) return;
        
        string hash = File.ReadAllText(GetOnlyOneFile(path, "hash"));
        string catalog = File.ReadAllText(GetOnlyOneFile(path, "json"));
        string allBundles = string.Empty;
        foreach ( string bundle in Directory.GetFiles(path, $"*.bundle"))
        {
            Debug.Log(Convert.ToBase64String(File.ReadAllBytes(bundle)));
            allBundles += $"____{Convert.ToBase64String(File.ReadAllBytes(bundle))}";
        }
        string allAddresses = string.Empty;
        // リソースロケータ（取得したカタログがデシリアライズされたもの）を取得
        IResourceLocator resourceLocator = await GetLocator(GetOnlyOneFile(path, "json"));
        // カタログ(メモリ上ではIResourceLocatorとして扱われる)からアドレス一覧を抽出
        // IResourceLocator は key(通常はAdress)と、それに紐づくアセットの対応関係の情報を持っている
        //foreach (var address in resourceLocator.Keys)
        //{
        //    Debug.Log(address);
        //    allAddresses += address;
        //}
        foreach (var label in labels)
        {
            if (label == "部屋" || label == "家具")
            {
                foreach (var address in await ExtractContentAdresses_Go(resourceLocator, label))
                {
                    Debug.Log(address);
                    allAddresses += $"____{address}";
                }
            }
            if (label == "アバター")
            {
                foreach (var address in await ExtractContentAdresses_Txt(resourceLocator, label))
                {
                    Debug.Log(address);
                    allAddresses += $"____{address}";
                }
            }
            if (label == "空")
            {
                foreach (var address in await ExtractContentAdresses_Sky(resourceLocator, label))
                {
                    Debug.Log(address);
                    allAddresses += $"____{address}";
                }
            }
        }

        Debug.Log(hash);
        Debug.Log(catalog);
        Debug.Log(allBundles);
        Debug.Log(allAddresses);
            
        //await Upload(catalog, hash, allBundles, allAddresses);

        await UploadInChunks(catalog, hash, allBundles, allAddresses, 500);
    }


    string GetOnlyOneFile(string dir, string extention)
    {
        // 指定した拡張子のファイル一覧を取得
        string[] files = Directory.GetFiles(dir, $"*.{extention}");
        // 該当ファイルが１つの場合のみ返す
        if(files.Length == 1)
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

    async UniTask UploadInChunks(string catalog, string hash, string allBundles, string allAddresses, int chunkSize)
    {
        JObject payloadObj = null;
        using (var sr = new StreamReader($@"{Application.dataPath}\Payload_UploadContents.json", System.Text.Encoding.UTF8))
        {
            payloadObj = JObject.Parse(sr.ReadToEnd());
            Debug.Log(payloadObj);
        }

        // カタログとハッシュ情報を設定
        payloadObj["properties"]["カタログ"]["rich_text"][0]["text"]["content"] = catalog;
        payloadObj["properties"]["ハッシュ"]["rich_text"][0]["text"]["content"] = hash;
        payloadObj["properties"]["全アドレス"]["rich_text"][0]["text"]["content"] = allAddresses;
        payloadObj["properties"]["コンテンツ名"]["title"][0]["text"]["content"] = DateTime.Now.ToString();

        // バンドルデータをチャンクに分割してアップロード
        int totalLength = allBundles.Length;
        int currentIndex = 0;

        while (currentIndex < totalLength)
        {
            // チャンクを取得（currentIndexからchunkSize分を切り取る）
            string chunk = allBundles.Substring(currentIndex, Math.Min(chunkSize, totalLength - currentIndex));

            // チャンクを設定
            payloadObj["properties"]["バンドル"]["rich_text"][0]["text"]["content"] = chunk;

            // JSONとしてシリアライズ
            string payload = JsonConvert.SerializeObject(payloadObj);
            Debug.Log($"{payload}");

            UnityWebRequest request = UnityWebRequest.PostWwwForm($"https://api.notion.com/v1/pages", payload);
            request.SetRequestHeader("Authorization", $"Bearer {NotionAccessToken}");
            request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Notion-Version", "2022-02-22");

            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"アップロード中にエラーが発生: {request.error}");
                break;
            }
            else if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"チャンク {currentIndex / chunkSize + 1} アップロード成功");
            }

            // インデックスを進める
            currentIndex += chunkSize;
        }
    }



    async UniTask Upload(string catalog, string hash, string allBundles, string allAddresses)
    {
        JObject payloadObj = null;
        using (var sr = new StreamReader($@"{Application.dataPath}\Payload_UploadContents.json", System.Text.Encoding.UTF8))
        {
            payloadObj = JObject.Parse(sr.ReadToEnd());
            Debug.Log(payloadObj);
        }
        payloadObj["properties"]["カタログ"]["rich_text"][0]["text"]["content"] = catalog;
        payloadObj["properties"]["ハッシュ"]["rich_text"][0]["text"]["content"] = hash;
        payloadObj["properties"]["バンドル"]["rich_text"][0]["text"]["content"] = allBundles;
        payloadObj["properties"]["全アドレス"]["rich_text"][0]["text"]["content"] = allAddresses;
        payloadObj["properties"]["コンテンツ名"]["title"][0]["text"]["content"] = DateTime.Now.ToString();
        string payload = JsonConvert.SerializeObject(payloadObj);
        Debug.Log($"{payload}");


        UnityWebRequest request = UnityWebRequest.PostWwwForm($"https://api.notion.com/v1/pages", payload);
        request.SetRequestHeader("Authorization", $"Bearer {NotionAccessToken}");
        request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Notion-Version", "2022-02-22");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();


        await request.SendWebRequest();



        switch (request.result)
        {
            case UnityWebRequest.Result.InProgress:
                Debug.Log("リクエスト中");
                break;

            case UnityWebRequest.Result.Success:
                Debug.Log("リクエスト成功");
                break;

            case UnityWebRequest.Result.ConnectionError:
                Debug.Log(
                    @"サーバとの通信に失敗。
                            リクエストが接続できなかった、
                            セキュリティで保護されたチャネルを確立できなかったなど。");
                Debug.LogError(request.error);
                break;

            case UnityWebRequest.Result.ProtocolError:
                Debug.Log(
                    @"サーバがエラー応答を返した。
                            サーバとの通信には成功したが、
                            接続プロトコルで定義されているエラーを受け取った。");
                Debug.LogError(request.error);
                break;

            case UnityWebRequest.Result.DataProcessingError:
                Debug.Log(
                    @"データの処理中にエラーが発生。
                            リクエストはサーバとの通信に成功したが、
                            受信したデータの処理中にエラーが発生。
                            データが破損しているか、正しい形式ではないなど。");
                Debug.LogError(request.error);
                break;

            default: throw new ArgumentOutOfRangeException();
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
                    Debug.Log($"{b.PrimaryKey} はロードできなかった");
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
