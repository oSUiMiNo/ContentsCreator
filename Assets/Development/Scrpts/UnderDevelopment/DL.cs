using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class DL : MonoBehaviour
{
    private const string NotionAccessToken = "secret_OIxSWO69mxnD9FNbmL2US0pcsLWCUmsaglBZBCWPWrC";
    //private const string DatabaseID = "e8777c71cf694fa8bd135fb1d7cb1728";
    private const string DatabaseID = "ee761ae157e346b88ef3fd58ba9146d3";

    public enum QueryType
    {
        ID,
        Address
    }
    [SerializeField] public QueryType queryType = QueryType.ID; 
    [SerializeField] public string searchWord;


    // Start is called before the first frame update
    async void Start()
    {
        await DownloadFilesFromDatabase(searchWord);
    }


    async UniTask DownloadFilesFromDatabase(string searchWord)
    {
        // クエリ検索
        JToken queryResult = await CallNotionAPI_QueryFile(searchWord, queryType);
        

        // クエリ結果が空かどうかを確認
        if (queryResult == null || !queryResult.Any())
        {
            Debug.LogError("指定されたタイトルに該当するエントリが見つかりませんでした。");
            return;
        }

        // Step 2: クエリ結果からファイルを取得する
        var firstElement = queryResult[0];
        var properties = firstElement["properties"] as JObject;

        if (properties == null)
        {
            Debug.LogError("プロパティが見つかりません。");
            return;
        }

        // カタログから .json ファイルを取得
        var catalogFile = properties["カタログ"];
        if (catalogFile != null && catalogFile["files"] is JArray catalogFiles && catalogFiles.Count > 0)
        {
            string catalogUrl = catalogFiles[0]["file"]["url"].ToString();
            string catalogName = catalogFiles[0]["name"].ToString();
            await DownloadFile(catalogUrl, catalogName);
        }
        else
        {
            Debug.LogError("カタログファイルが見つかりません。");
        }

        // ハッシュから .hash ファイルを取得
        var hashFile = properties["ハッシュ"];
        if (hashFile != null && hashFile["files"] is JArray hashFiles && hashFiles.Count > 0)
        {
            string hashUrl = hashFiles[0]["file"]["url"].ToString();
            string hashName = hashFiles[0]["name"].ToString();
            await DownloadFile(hashUrl, hashName);
        }
        else
        {
            Debug.LogError("ハッシュファイルが見つかりません。");
        }

        // バンドルから .bundle ファイルを複数取得
        var bundleFile = properties["バンドル"];
        if (bundleFile != null && bundleFile["files"] is JArray bundleFiles && bundleFiles.Count > 0)
        {
            foreach (var file in bundleFiles)
            {
                string bundleUrl = file["file"]["url"].ToString();
                string bundleName = file["name"].ToString();
                await DownloadFile(bundleUrl, bundleName);
            }
        }
        else
        {
            Debug.LogError("バンドルファイルが見つかりません。");
        }
    }

    // ファイルをダウンロードする共通関数
    async UniTask DownloadFile(string fileUrl, string fileName)
    {
        string newFilePath = @$"C:\Users\{Environment.UserName}\Downloads\{fileName}";

        using (UnityWebRequest request = new UnityWebRequest(fileUrl))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                byte[] results = request.downloadHandler.data;
                using (FileStream fs = new FileStream(newFilePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    fs.Write(results, 0, results.Length);
                }
                Debug.Log($"{fileName} が {newFilePath} に保存されました。");
            }
        }
    }


    


    async UniTask<JToken> CallNotionAPI_QueryFile(string searchWord, QueryType queryType)
    {
        // クエリを構築
        var queryPayload_ID = new
        {
            filter = new
            {
                property = "ID",  // 正しいプロパティ名
                title = new
                {
                    contains = searchWord // 検索ワードを含むものをフィルタ
                }
            }
        };

        // クエリを構築
        var queryPayload_Address = new
        {
            filter = new
            {
                property = "全アドレス",  // プロパティを "全アドレス" に設定
                rich_text = new  // テキストフィールドを検索
                {
                    contains = searchWord // 検索ワードを含むものをフィルタ
                }
            }
        };

        // JSON形式にシリアライズ
        string jsonQuery = "";
        switch (queryType)
        {
            case QueryType.ID:
                jsonQuery = JsonConvert.SerializeObject(queryPayload_ID);
                break;
            case QueryType.Address:
                jsonQuery = JsonConvert.SerializeObject(queryPayload_Address);
                break;
            default:
                Debug.LogAssertion("クエリタイプを選択して");
                break;
        }
        if (string.IsNullOrEmpty(jsonQuery)) return null;

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonQuery);

        using (UnityWebRequest request = new UnityWebRequest($"https://api.notion.com/v1/databases/{DatabaseID}/query", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {NotionAccessToken}");
            request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
            request.SetRequestHeader("Notion-Version", "2022-02-22");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
                return null;
            }

            string jsonStr = request.downloadHandler.text;

            // パース
            JObject responseObj = JObject.Parse(jsonStr);

            // テーブルのフィルタリングされた要素
            JArray elements = (JArray)responseObj["results"];
            if (elements == null || !elements.Any())
            {
                Debug.LogError("結果が見つかりませんでした。");
                return null;
            }

            // elements の中身をログに表示
            foreach (var element in elements)
            {
                Debug.Log(element.ToString());

                // アセット名の存在確認
                if (element["properties"]?["全アドレス"]?["title"]?[0]?["text"]?["content"] != null)
                {
                    Debug.Log($"ID: {element["id"]}, Name: {element["properties"]["全アドレス"]["title"][0]["text"]["content"]}");
                }
                else
                {
                    Debug.LogError("アセット名が見つかりません。");
                }

                // 各ファイルの存在確認
                var catalogFile = element["properties"]?["カタログ"];
                if (catalogFile != null && catalogFile["files"] is JArray catalogFiles && catalogFiles.Count > 0)
                {
                    Debug.Log($"カタログファイル名: {catalogFiles[0]["name"]}");
                }
                else
                {
                    Debug.LogError("カタログファイルが見つかりません。");
                }

                var hashFile = element["properties"]?["ハッシュ"];
                if (hashFile != null && hashFile["files"] is JArray hashFiles && hashFiles.Count > 0)
                {
                    Debug.Log($"ハッシュファイル名: {hashFiles[0]["name"]}");
                }
                else
                {
                    Debug.LogError("ハッシュファイルが見つかりません。");
                }

                var bundleFile = element["properties"]?["バンドル"];
                if (bundleFile != null && bundleFile["files"] is JArray bundleFiles && bundleFiles.Count > 0)
                {
                    foreach (var file in bundleFiles)
                    {
                        Debug.Log($"バンドルファイル名: {file["name"]}");
                    }
                }
                else
                {
                    Debug.LogError("バンドルファイルが見つかりません。");
                }
            }

            return elements;
        }
    }

}
