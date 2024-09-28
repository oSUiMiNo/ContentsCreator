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


// .bundle�t�@�C�����������ăA�b�v�ł��Ȃ�
public class Uploader : MonoBehaviour
{
    private const string NotionAccessToken = "secret_OIxSWO69mxnD9FNbmL2US0pcsLWCUmsaglBZBCWPWrC"; //�V������

    public List<string> labels = new List<string>
    {
        "����",
        "�A�o�^�[",
        "���[�V����",
        "��",
        "�Ƌ�"
    };



    private async void Start()
    {
        string path = @$"{Application.dataPath.Replace("Assets", "")}{Addressables.BuildPath}/StandaloneWindows64";
        Debug.Log($"�p�X{path}");

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
        // ���\�[�X���P�[�^�i�擾�����J�^���O���f�V���A���C�Y���ꂽ���́j���擾
        IResourceLocator resourceLocator = await GetLocator(GetOnlyOneFile(path, "json"));
        // �J�^���O(��������ł�IResourceLocator�Ƃ��Ĉ�����)����A�h���X�ꗗ�𒊏o
        // IResourceLocator �� key(�ʏ��Adress)�ƁA����ɕR�Â��A�Z�b�g�̑Ή��֌W�̏��������Ă���
        //foreach (var address in resourceLocator.Keys)
        //{
        //    Debug.Log(address);
        //    allAddresses += address;
        //}
        foreach (var label in labels)
        {
            if (label == "����" || label == "�Ƌ�")
            {
                foreach (var address in await ExtractContentAdresses_Go(resourceLocator, label))
                {
                    Debug.Log(address);
                    allAddresses += $"____{address}";
                }
            }
            if (label == "�A�o�^�[")
            {
                foreach (var address in await ExtractContentAdresses_Txt(resourceLocator, label))
                {
                    Debug.Log(address);
                    allAddresses += $"____{address}";
                }
            }
            if (label == "��")
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
        // �w�肵���g���q�̃t�@�C���ꗗ���擾
        string[] files = Directory.GetFiles(dir, $"*.{extention}");
        // �Y���t�@�C�����P�̏ꍇ�̂ݕԂ�
        if(files.Length == 1)
        {
            return files[0];
        }
        else
        if (files.Length == 0)
        {
            Debug.LogAssertion($"�w�肵���t�H���_ {dir} �Ɂu.{extention}�v�t�@�C���͖���");
            return null;
        }
        else
        {
            Debug.LogAssertion($"�w�肵���t�H���_ {dir} �Ɂu.{extention}�v�t�@�C������������");
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

        // �J�^���O�ƃn�b�V������ݒ�
        payloadObj["properties"]["�J�^���O"]["rich_text"][0]["text"]["content"] = catalog;
        payloadObj["properties"]["�n�b�V��"]["rich_text"][0]["text"]["content"] = hash;
        payloadObj["properties"]["�S�A�h���X"]["rich_text"][0]["text"]["content"] = allAddresses;
        payloadObj["properties"]["�R���e���c��"]["title"][0]["text"]["content"] = DateTime.Now.ToString();

        // �o���h���f�[�^���`�����N�ɕ������ăA�b�v���[�h
        int totalLength = allBundles.Length;
        int currentIndex = 0;

        while (currentIndex < totalLength)
        {
            // �`�����N���擾�icurrentIndex����chunkSize����؂���j
            string chunk = allBundles.Substring(currentIndex, Math.Min(chunkSize, totalLength - currentIndex));

            // �`�����N��ݒ�
            payloadObj["properties"]["�o���h��"]["rich_text"][0]["text"]["content"] = chunk;

            // JSON�Ƃ��ăV���A���C�Y
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
                Debug.LogError($"�A�b�v���[�h���ɃG���[������: {request.error}");
                break;
            }
            else if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"�`�����N {currentIndex / chunkSize + 1} �A�b�v���[�h����");
            }

            // �C���f�b�N�X��i�߂�
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
        payloadObj["properties"]["�J�^���O"]["rich_text"][0]["text"]["content"] = catalog;
        payloadObj["properties"]["�n�b�V��"]["rich_text"][0]["text"]["content"] = hash;
        payloadObj["properties"]["�o���h��"]["rich_text"][0]["text"]["content"] = allBundles;
        payloadObj["properties"]["�S�A�h���X"]["rich_text"][0]["text"]["content"] = allAddresses;
        payloadObj["properties"]["�R���e���c��"]["title"][0]["text"]["content"] = DateTime.Now.ToString();
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
                Debug.Log("���N�G�X�g��");
                break;

            case UnityWebRequest.Result.Success:
                Debug.Log("���N�G�X�g����");
                break;

            case UnityWebRequest.Result.ConnectionError:
                Debug.Log(
                    @"�T�[�o�Ƃ̒ʐM�Ɏ��s�B
                            ���N�G�X�g���ڑ��ł��Ȃ������A
                            �Z�L�����e�B�ŕی삳�ꂽ�`���l�����m���ł��Ȃ������ȂǁB");
                Debug.LogError(request.error);
                break;

            case UnityWebRequest.Result.ProtocolError:
                Debug.Log(
                    @"�T�[�o���G���[������Ԃ����B
                            �T�[�o�Ƃ̒ʐM�ɂ͐����������A
                            �ڑ��v���g�R���Œ�`����Ă���G���[���󂯎�����B");
                Debug.LogError(request.error);
                break;

            case UnityWebRequest.Result.DataProcessingError:
                Debug.Log(
                    @"�f�[�^�̏������ɃG���[�������B
                            ���N�G�X�g�̓T�[�o�Ƃ̒ʐM�ɐ����������A
                            ��M�����f�[�^�̏������ɃG���[�������B
                            �f�[�^���j�����Ă��邩�A�������`���ł͂Ȃ��ȂǁB");
                Debug.LogError(request.error);
                break;

            default: throw new ArgumentOutOfRangeException();
        }
    }


    // �g�������J�^���O�����\�[�X���P�[�^�ɓo�^
    public async UniTask<IResourceLocator> GetLocator(string catalogPath)
    {
        // �V�����J�^���O���擾�B�t�@�C���p�X��URL
        AsyncOperationHandle<IResourceLocator> requestCatalog
            = Addressables.LoadContentCatalogAsync(catalogPath);

        // ���[�h������҂�
        await requestCatalog;

        // ���̃G���[��������
        Assert.AreEqual(AsyncOperationStatus.Succeeded, requestCatalog.Status);

        // ���\�[�X���P�[�^�i�擾�����J�^���O���f�V���A���C�Y���ꂽ���́j��Ԃ�
        return requestCatalog.Result;
    }


    // �Y���R���e���c�̃A�h���X�ꗗ���擾
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
            Debug.Log($"���̃J�^���O�Ƀ��x�� {label} �͖���");
            return new List<string>();
        }


        // �J�^���O(��������ł�IResourceLocator�Ƃ��Ĉ�����)����A�h���X�ꗗ�𒊏o
        // IResourceLocator �� key(�ʏ��Adress)�ƁA����ɕR�Â��A�Z�b�g�̑Ή��֌W�̏��������Ă���
        foreach (var a in resourceLocator.Keys)
        {
            addresses.Add(a.ToString());
        }

        // ���[�h�ł�����̂𔻕ʂ��āA���񃍁[�h�������^�C�v�̃A�Z�b�g�����ʂ��āA�S�����[�h
        foreach (var a in addresses)
        {
            Debug.Log($"------ �A�h���X : {a}");
            IList<IResourceLocation> resourceLocations;
            // IResourceLocation �̓A�Z�b�g�����[�h���邽�߂ɕK�v�ȏ��������Ă���
            // IResourceLocator �� Locate() �ɓ���̃A�h���X��n���� IResourceLocation ���Ԃ��Ă���
            // �܂�J�^���O�� IResourceLocation �����Ă���
            if (!resourceLocator.Locate(a, typeof(GameObject), out resourceLocations)) continue;


            foreach (var b in resourceLocations)
            {
                Debug.Log($"------- �v���C�}���[�L�[ {b.PrimaryKey}");
                // IResourceLocation �� PrimaryKey ���A�h���X�炵��

                AsyncOperationHandle<GameObject> opHandle_LoadedGObj = new AsyncOperationHandle<GameObject>();
                try
                {
                    opHandle_LoadedGObj = Addressables.LoadAssetAsync<GameObject>(b.PrimaryKey);
                    await opHandle_LoadedGObj;
                }
                catch (Exception e)
                {
                    Debug.Log($"{b.PrimaryKey} �̓��[�h�ł��Ȃ�����");
                    continue;
                }

                // ����̃��x���Ɋ܂܂�Ă���Q�[���I�u�W�F�N�g���ǂ���
                bool containedInLabel = false;
                foreach (GameObject labelGobj in handle_LabelGobj.Result)
                {
                    if (opHandle_LoadedGObj.Result == labelGobj)
                    {
                        Debug.Log($"���x���Ɋ܂܂�Ă��� {opHandle_LoadedGObj.Result}");
                        containedInLabel = true;
                        continue;
                    }
                }
                if (containedInLabel == false) continue;

                // ���[�h�ł��Ȃ������玟�̃��[�v��
                if (opHandle_LoadedGObj.Status != AsyncOperationStatus.Succeeded) continue;
                //Debug.Log($"------ ���[�h�����Q�[���I�u�W�F�N�g : {opHandle_LoadedGObj.Result}");

                // �V�[�����̕��̂Ƃ��ẴQ�[���I�u�W�F�N�g(�܂�Renderer�R���|�[�l���g�����Ă���͂�)���ǂ����𔻕ʂ��āA
                // ������玟�̃��[�v��
                Renderer[] renderers = opHandle_LoadedGObj.Result.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                //Debug.Log($"------ �����_�������Q�[���I�u�W�F�N�g : {opHandle_LoadedGObj.Result}");

                // ���Ƀ��[�h�ς݂̃A�h���X�Ɋ܂܂�Ă����玟�̃��[�v��
                if (loadedAddresses.Contains(b.PrimaryKey)) continue;
                // ����̃A�h���X���A���[�h�ς݂̃A�h���X�ꗗloadedAddresses�ɋL�^
                loadedAddresses.Add(b.PrimaryKey);
            }
        }

        Addressables.RemoveResourceLocator(resourceLocator);
        return loadedAddresses;
    }

    // �Y���R���e���c�̃A�h���X�ꗗ���擾
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
            Debug.Log($"���̃J�^���O�Ƀ��x�� {label} �͖���");
            return new List<string>();
        }


        // �J�^���O(��������ł�IResourceLocator�Ƃ��Ĉ�����)����A�h���X�ꗗ�𒊏o
        // IResourceLocator �� key(�ʏ��Adress)�ƁA����ɕR�Â��A�Z�b�g�̑Ή��֌W�̏��������Ă���
        foreach (var a in resourceLocator.Keys)
        {
            addresses.Add(a.ToString());
        }

        // ���[�h�ł�����̂𔻕ʂ��āA���񃍁[�h�������^�C�v�̃A�Z�b�g�����ʂ��āA�S�����[�h
        foreach (var a in addresses)
        {
            Debug.Log($"------ �A�h���X : {a}");
            IList<IResourceLocation> resourceLocations;
            // IResourceLocation �̓A�Z�b�g�����[�h���邽�߂ɕK�v�ȏ��������Ă���
            // IResourceLocator �� Locate() �ɓ���̃A�h���X��n���� IResourceLocation ���Ԃ��Ă���
            // �܂�J�^���O�� IResourceLocation �����Ă���
            if (!resourceLocator.Locate(a, typeof(TextAsset), out resourceLocations)) continue;


            foreach (var b in resourceLocations)
            {
                Debug.Log($"------- �v���C�}���[�L�[ {b.PrimaryKey}");
                // IResourceLocation �� PrimaryKey ���A�h���X�炵��

                AsyncOperationHandle<TextAsset> opHandle_LoadedGObj = new AsyncOperationHandle<TextAsset>();
                try
                {
                    opHandle_LoadedGObj = Addressables.LoadAssetAsync<TextAsset>(b.PrimaryKey);
                    await opHandle_LoadedGObj;
                }
                catch (Exception e)
                {
                    Debug.Log($"{b.PrimaryKey} �̓��[�h�ł��Ȃ�����");
                    continue;
                }

                // ����̃��x���Ɋ܂܂�Ă���Q�[���I�u�W�F�N�g���ǂ���
                bool containedInLabel = false;
                foreach (TextAsset labelGobj in handle_LabelGobj.Result)
                {
                    if (opHandle_LoadedGObj.Result == labelGobj)
                    {
                        Debug.Log($"���x���Ɋ܂܂�Ă��� {opHandle_LoadedGObj.Result}");
                        containedInLabel = true;
                        continue;
                    }
                }
                if (containedInLabel == false) continue;

                // ���[�h�ł��Ȃ������玟�̃��[�v��
                if (opHandle_LoadedGObj.Status != AsyncOperationStatus.Succeeded) continue;
                //Debug.Log($"------ ���[�h�����Q�[���I�u�W�F�N�g : {opHandle_LoadedGObj.Result}");

                // ���Ƀ��[�h�ς݂̃A�h���X�Ɋ܂܂�Ă����玟�̃��[�v��
                if (loadedAddresses.Contains(b.PrimaryKey)) continue;
                // ����̃A�h���X���A���[�h�ς݂̃A�h���X�ꗗloadedAddresses�ɋL�^
                loadedAddresses.Add(b.PrimaryKey);
            }
        }

        Addressables.RemoveResourceLocator(resourceLocator);
        return loadedAddresses;
    }

    // �Y���R���e���c�̃A�h���X�ꗗ���擾
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
            Debug.Log($"���̃J�^���O�Ƀ��x�� {label} �͖���");
            return new List<string>();
        }


        // �J�^���O(��������ł�IResourceLocator�Ƃ��Ĉ�����)����A�h���X�ꗗ�𒊏o
        // IResourceLocator �� key(�ʏ��Adress)�ƁA����ɕR�Â��A�Z�b�g�̑Ή��֌W�̏��������Ă���
        foreach (var a in resourceLocator.Keys)
        {
            addresses.Add(a.ToString());
        }

        // ���[�h�ł�����̂𔻕ʂ��āA���񃍁[�h�������^�C�v�̃A�Z�b�g�����ʂ��āA�S�����[�h
        foreach (var a in addresses)
        {
            Debug.Log($"------ �A�h���X : {a}");
            IList<IResourceLocation> resourceLocations;
            // IResourceLocation �̓A�Z�b�g�����[�h���邽�߂ɕK�v�ȏ��������Ă���
            // IResourceLocator �� Locate() �ɓ���̃A�h���X��n���� IResourceLocation ���Ԃ��Ă���
            // �܂�J�^���O�� IResourceLocation �����Ă���
            if (!resourceLocator.Locate(a, typeof(Material), out resourceLocations)) continue;


            foreach (var b in resourceLocations)
            {
                Debug.Log($"------- �v���C�}���[�L�[ {b.PrimaryKey}");
                // IResourceLocation �� PrimaryKey ���A�h���X�炵��

                AsyncOperationHandle<Material> opHandle_LoadedGObj = new AsyncOperationHandle<Material>();
                try
                {
                    opHandle_LoadedGObj = Addressables.LoadAssetAsync<Material>(b.PrimaryKey);
                    await opHandle_LoadedGObj;
                }
                catch (Exception e)
                {
                    Debug.Log($"{b.PrimaryKey} �̓��[�h�ł��Ȃ�����");
                    continue;
                }

                // ����̃��x���Ɋ܂܂�Ă���Q�[���I�u�W�F�N�g���ǂ���
                bool containedInLabel = false;
                foreach (Material labelGobj in handle_LabelGobj.Result)
                {
                    if (opHandle_LoadedGObj.Result == labelGobj)
                    {
                        Debug.Log($"���x���Ɋ܂܂�Ă��� {opHandle_LoadedGObj.Result}");
                        containedInLabel = true;
                        continue;
                    }
                }
                if (containedInLabel == false) continue;

                // ���[�h�ł��Ȃ������玟�̃��[�v��
                if (opHandle_LoadedGObj.Status != AsyncOperationStatus.Succeeded) continue;
                //Debug.Log($"------ ���[�h�����Q�[���I�u�W�F�N�g : {opHandle_LoadedGObj.Result}");

                // ���Ƀ��[�h�ς݂̃A�h���X�Ɋ܂܂�Ă����玟�̃��[�v��
                if (loadedAddresses.Contains(b.PrimaryKey)) continue;
                // ����̃A�h���X���A���[�h�ς݂̃A�h���X�ꗗloadedAddresses�ɋL�^
                loadedAddresses.Add(b.PrimaryKey);
            }
        }

        Addressables.RemoveResourceLocator(resourceLocator);
        return loadedAddresses;
    }
}
