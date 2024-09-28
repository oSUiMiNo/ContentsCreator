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
        // �N�G������
        JToken queryResult = await CallNotionAPI_QueryFile(searchWord, queryType);
        

        // �N�G�����ʂ��󂩂ǂ������m�F
        if (queryResult == null || !queryResult.Any())
        {
            Debug.LogError("�w�肳�ꂽ�^�C�g���ɊY������G���g����������܂���ł����B");
            return;
        }

        // Step 2: �N�G�����ʂ���t�@�C�����擾����
        var firstElement = queryResult[0];
        var properties = firstElement["properties"] as JObject;

        if (properties == null)
        {
            Debug.LogError("�v���p�e�B��������܂���B");
            return;
        }

        // �J�^���O���� .json �t�@�C�����擾
        var catalogFile = properties["�J�^���O"];
        if (catalogFile != null && catalogFile["files"] is JArray catalogFiles && catalogFiles.Count > 0)
        {
            string catalogUrl = catalogFiles[0]["file"]["url"].ToString();
            string catalogName = catalogFiles[0]["name"].ToString();
            await DownloadFile(catalogUrl, catalogName);
        }
        else
        {
            Debug.LogError("�J�^���O�t�@�C����������܂���B");
        }

        // �n�b�V������ .hash �t�@�C�����擾
        var hashFile = properties["�n�b�V��"];
        if (hashFile != null && hashFile["files"] is JArray hashFiles && hashFiles.Count > 0)
        {
            string hashUrl = hashFiles[0]["file"]["url"].ToString();
            string hashName = hashFiles[0]["name"].ToString();
            await DownloadFile(hashUrl, hashName);
        }
        else
        {
            Debug.LogError("�n�b�V���t�@�C����������܂���B");
        }

        // �o���h������ .bundle �t�@�C���𕡐��擾
        var bundleFile = properties["�o���h��"];
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
            Debug.LogError("�o���h���t�@�C����������܂���B");
        }
    }

    // �t�@�C�����_�E�����[�h���鋤�ʊ֐�
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
                Debug.Log($"{fileName} �� {newFilePath} �ɕۑ�����܂����B");
            }
        }
    }


    


    async UniTask<JToken> CallNotionAPI_QueryFile(string searchWord, QueryType queryType)
    {
        // �N�G�����\�z
        var queryPayload_ID = new
        {
            filter = new
            {
                property = "ID",  // �������v���p�e�B��
                title = new
                {
                    contains = searchWord // �������[�h���܂ނ��̂��t�B���^
                }
            }
        };

        // �N�G�����\�z
        var queryPayload_Address = new
        {
            filter = new
            {
                property = "�S�A�h���X",  // �v���p�e�B�� "�S�A�h���X" �ɐݒ�
                rich_text = new  // �e�L�X�g�t�B�[���h������
                {
                    contains = searchWord // �������[�h���܂ނ��̂��t�B���^
                }
            }
        };

        // JSON�`���ɃV���A���C�Y
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
                Debug.LogAssertion("�N�G���^�C�v��I������");
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

            // �p�[�X
            JObject responseObj = JObject.Parse(jsonStr);

            // �e�[�u���̃t�B���^�����O���ꂽ�v�f
            JArray elements = (JArray)responseObj["results"];
            if (elements == null || !elements.Any())
            {
                Debug.LogError("���ʂ�������܂���ł����B");
                return null;
            }

            // elements �̒��g�����O�ɕ\��
            foreach (var element in elements)
            {
                Debug.Log(element.ToString());

                // �A�Z�b�g���̑��݊m�F
                if (element["properties"]?["�S�A�h���X"]?["title"]?[0]?["text"]?["content"] != null)
                {
                    Debug.Log($"ID: {element["id"]}, Name: {element["properties"]["�S�A�h���X"]["title"][0]["text"]["content"]}");
                }
                else
                {
                    Debug.LogError("�A�Z�b�g����������܂���B");
                }

                // �e�t�@�C���̑��݊m�F
                var catalogFile = element["properties"]?["�J�^���O"];
                if (catalogFile != null && catalogFile["files"] is JArray catalogFiles && catalogFiles.Count > 0)
                {
                    Debug.Log($"�J�^���O�t�@�C����: {catalogFiles[0]["name"]}");
                }
                else
                {
                    Debug.LogError("�J�^���O�t�@�C����������܂���B");
                }

                var hashFile = element["properties"]?["�n�b�V��"];
                if (hashFile != null && hashFile["files"] is JArray hashFiles && hashFiles.Count > 0)
                {
                    Debug.Log($"�n�b�V���t�@�C����: {hashFiles[0]["name"]}");
                }
                else
                {
                    Debug.LogError("�n�b�V���t�@�C����������܂���B");
                }

                var bundleFile = element["properties"]?["�o���h��"];
                if (bundleFile != null && bundleFile["files"] is JArray bundleFiles && bundleFiles.Count > 0)
                {
                    foreach (var file in bundleFiles)
                    {
                        Debug.Log($"�o���h���t�@�C����: {file["name"]}");
                    }
                }
                else
                {
                    Debug.LogError("�o���h���t�@�C����������܂���B");
                }
            }

            return elements;
        }
    }

}
