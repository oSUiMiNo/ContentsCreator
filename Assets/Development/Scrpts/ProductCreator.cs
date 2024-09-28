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





[CustomEditor(typeof(ProductCreator))]//�g������N���X���w��
public class ExampleScriptEditor : Editor
{
    // Inspector��GUI���X�V
    public override void OnInspectorGUI()
    {
        //����Inspector������\��
        base.OnInspectorGUI();

        //target��ϊ����đΏۂ��擾
        ProductCreator creator = target as ProductCreator;

        // �{�^���̃X�^�C�����`
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 48; // �{�^���̕����T�C�Y��ݒ�
        buttonStyle.alignment = TextAnchor.MiddleCenter; // �{�^���̕�����^�񒆂�
        buttonStyle.fontStyle = FontStyle.Bold; // �����𑾂��ݒ�

        // �{�^���̔w�i�F��ɐݒ�
        Color originalColor = GUI.backgroundColor;

        // �c���тɂ���
        GUILayout.BeginVertical();

        // �����ŉ����т��t���L�V�u���X�y�[�X��ǉ����ă{�^���S�̂��E��
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // �{�^���̔w�i�F�����F�ɐݒ肵�čŏ��̃{�^��
        //GUI.backgroundColor = Color.yellow;
        //if (GUILayout.Button("Build", buttonStyle, GUILayout.Width(190), GUILayout.Height(80)))
        //{
        //    creator.BuildAddressableContent();
        //}

        GUILayout.EndHorizontal(); // �ŏ��̃{�^���̉E�񂹏����I��

        // ������̃{�^�������l�ɉE�񂹏���
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // �{�^���̔w�i�F��ɐݒ肵�Ď��̃{�^��
        GUI.backgroundColor = Color.blue;
        if (GUILayout.Button("Export", buttonStyle, GUILayout.Width(190), GUILayout.Height(80)))
        {
            //creator.ExportFiles();
            creator.ExportShiji();
        }

        GUILayout.EndHorizontal(); // 2�ڂ̃{�^���̉E�񂹏����I��

        // �c���яI��
        GUILayout.EndVertical();

        // ���̐F�ɖ߂�
        GUI.backgroundColor = originalColor;
    }
}






[CreateAssetMenu(fileName = "ProductCreator", menuName = "Scriptable Objects/ProductCreator")]
public class ProductCreator : ScriptableObject
{
    [SerializeField] string �t�H���_��;

    private const string NotionAccessToken = "secret_OIxSWO69mxnD9FNbmL2US0pcsLWCUmsaglBZBCWPWrC"; //�V������

    string path;

    List<string> labels = new List<string>
    {
        "����",
        "�A�o�^�[",
        "���[�V����",
        "��",
        "�Ƌ�"
    };

   
    // �G�N�X�|�[�g
    public async void ExportShiji()
    {
        ClearConsole();
        AssetDatabase.Refresh();

        if (string.IsNullOrEmpty(�t�H���_��))
        {
            Debug.LogAssertion("�t�@�C���������߂Ă�������");
            return;
        }

        path = @$"{Application.dataPath.Replace("Assets", "")}{Addressables.BuildPath}/StandaloneWindows64";
        //Debug.Log(GetOnlyOneFile(path, "json"));

        Debug.Log("�r���h�J�n");
        BuildAddressableContent();


        Debug.Log("�r���h����");

        Debug.Log("�G�N�X�|�[�g�J�n");

        if (!Directory.Exists(path)) return;
        Debug.Log($"�p�X{path}");
       
        Directory.CreateDirectory(@$"{Application.dataPath}/Publish/{�t�H���_��}");
        await Delay.Frame(1);
        AssetDatabase.Refresh();

        Debug.Log($"�p�[�V�X�e���g{Application.persistentDataPath}");

        string addresses = string.Empty;
        // ���\�[�X���P�[�^�i�擾�����J�^���O���f�V���A���C�Y���ꂽ���́j���擾
        IResourceLocator resourceLocator = await GetLocator(GetOnlyOneFile(path, "json"));
        // �J�^���O(��������ł�IResourceLocator�Ƃ��Ĉ�����)����A�h���X�ꗗ�𒊏o
        // IResourceLocator �� key(�ʏ��Adress)�ƁA����ɕR�Â��A�Z�b�g�̑Ή��֌W�̏��������Ă���
        //foreach (var address in resourceLocator.Keys)
        //{
        //    Debug.Log(address);
        //    allAddresses += address;
        //}
        Debug.Log($"0");

        foreach (var label in labels)
        {
            Debug.Log($"1");

            if (label == "����" || label == "�Ƌ�")
            {
                foreach (var address in await ExtractContentAdresses_Go(resourceLocator, label))
                {
                    Debug.Log(address);
                    addresses += $"{address}|||";
                }
            }
            if (label == "�A�o�^�[")
            {
                foreach (var address in await ExtractContentAdresses_Txt(resourceLocator, label))
                {
                    Debug.Log(address);
                    addresses += $"{address}|||";
                }
            }
            if (label == "��")
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


        // .bundle�t�@�C���̒��g�𕶎���Ƃ��ēǂݍ���
        List<Name_Content> bundles = new List<Name_Content>();
        foreach (string bundle in Directory.GetFiles(path, $"*.bundle"))
        {
            // �o�C�i���t�@�C�����o�C�g�z��Ƃ��ēǂݍ���
            byte[] bundleBytes = File.ReadAllBytes(bundle);
            // �o�C�g�z���Base64������ɕϊ����Ēǉ�����i�o�C�i���t�@�C���̂܂܈��������ꍇ�j
            string bundleString = Convert.ToBase64String(bundleBytes);

            Name_Content bundleAsTxt = new Name_Content()
            {
                FileName = System.IO.Path.GetFileName(bundle),
                Content = bundleString
            };

            bundles.Add(bundleAsTxt);
        }
        Debug.Log($"3");

        // �V���A���C�Y����f�[�^���쐬
        Shiji shiji = new Shiji
        {
            Hash = hash,
            Catalog = catalog,
            Addresses = addresses,
            Bundles = bundles
        };


        // �V���A���C�Y
        string jsonString = JsonConvert.SerializeObject(shiji, Formatting.Indented);
        Debug.Log(jsonString);

        //// �o�C�g�z��ɕϊ�
        //byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);


        string shijiPath = @$"{Application.dataPath}/Publish/{�t�H���_��}/{hash.Content}.shiji";
        Debug.Log($"4");

        // �t�@�C���ɏ�������
        File.WriteAllText(shijiPath, jsonString);

        //// .bytes �t�@�C���� .shizi �t�@�C���Ƃ��ăR�s�[
        //File.Copy(bytesPath, shiziPath, true); // �㏑������

        AssetDatabase.Refresh();
        Debug.Log("�G�N�X�|�[�g����");
    }


    //public async void ExportFiles()
    //{
    //    if (string.IsNullOrEmpty(�t�H���_��))
    //    {
    //        Debug.LogAssertion("�t�H���_�������߂Ă�������");
    //        return;
    //    }

    //    string path = @$"{Application.dataPath.Replace("Assets", "")}{Addressables.BuildPath}/StandaloneWindows64";
    //    string newPath = @$"{Application.dataPath}/Publish/{�t�H���_��}";
    //    if (!Directory.Exists(path)) return;
    //    Debug.Log($"�p�X{path}");

    //    Debug.Log($"�p�[�V�X�e���g{Application.persistentDataPath}");

    //    string allAddresses = string.Empty;
    //    // ���\�[�X���P�[�^�i�擾�����J�^���O���f�V���A���C�Y���ꂽ���́j���擾
    //    IResourceLocator resourceLocator = await GetLocator(GetOnlyOneFile(path, "json"));
    //    // �J�^���O(��������ł�IResourceLocator�Ƃ��Ĉ�����)����A�h���X�ꗗ�𒊏o
    //    // IResourceLocator �� key(�ʏ��Adress)�ƁA����ɕR�Â��A�Z�b�g�̑Ή��֌W�̏��������Ă���
    //    //foreach (var address in resourceLocator.Keys)
    //    //{
    //    //    Debug.Log(address);
    //    //    allAddresses += address;
    //    //}
    //    foreach (var label in labels)
    //    {
    //        if (label == "����" || label == "�Ƌ�")
    //        {
    //            foreach (var address in await ExtractContentAdresses_Go(resourceLocator, label))
    //            {
    //                Debug.Log(address);
    //                allAddresses += $"{address}|||";
    //            }
    //        }
    //        if (label == "�A�o�^�[")
    //        {
    //            foreach (var address in await ExtractContentAdresses_Txt(resourceLocator, label))
    //            {
    //                Debug.Log(address);
    //                allAddresses += $"{address}|||";
    //            }
    //        }
    //        if (label == "��")
    //        {
    //            foreach (var address in await ExtractContentAdresses_Sky(resourceLocator, label))
    //            {
    //                Debug.Log(address);
    //                allAddresses += $"{address}|||";
    //            }
    //        }
    //    }
    //    Debug.Log(allAddresses);


    //    // �J�^���O���������Ă���f�B���N�g���̒��g�����v���W�F�N�g�̃t�H���_���ɕ���
    //    DirectoryUtil.CopyParentFolder(path, newPath);

    //    foreach (string bundle in Directory.GetFiles(newPath, $"*.bundle"))
    //    {
    //        // .bundle�t�@�C�������擾���A�g���q�� .bytes �ɕύX
    //        string bytesFilePath = Path.ChangeExtension(bundle, ".bytes");
    //        // .bundle �t�@�C���� .bytes �t�@�C���Ƃ��ăR�s�[
    //        File.Copy(bundle, bytesFilePath, true); // �㏑������
    //        // ���� .bundle �t�@�C�����폜
    //        File.Delete(bundle);
    //    }


    //    try
    //    {
    //        // �t�@�C���ɕ��������������
    //        File.WriteAllText(@$"{newPath}/Addresses.txt", allAddresses);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogAssertion("�G���[���������܂���: " + ex.Message);
    //    }
    //    try
    //    {
    //        // �t�@�C���ɕ��������������
    //        File.WriteAllText(@$"{newPath}/Hash.txt", File.ReadAllText(Directory.GetFiles(newPath, $"*.hash")[0]));
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogAssertion("�G���[���������܂���: " + ex.Message);
    //    }
    //    try
    //    {
    //        // �t�@�C���ɕ��������������
    //        File.WriteAllText(@$"{newPath}/ID.txt", Guid.NewGuid().ToString());
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogAssertion("�G���[���������܂���: " + ex.Message);
    //    }

    //    AssetDatabase.Refresh();
    //}


    // Addressable�̃r���h����



    [MenuItem("Tools/Clear Console %#c")]
    static void ClearConsole()
    {
        //var logEntries = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
        //var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        //clearMethod.Invoke(null, null);

        // Unity�G�f�B�^�[��ŃR���\�[�����N���A
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }


    public async void BuildAddressableContent()
    {
        Debug.Log("Addressable�r���h���J�n���܂�...");
        // Addressables�̃L���b�V�����N���A
        AddressableAssetSettings.CleanPlayerContent();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetSettings.BuildPlayerContent();
        // �J�^���O�����[�h����i��j
        AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(GetOnlyOneFile(path, "json"));
        await handle;

        // ������ handle ���g�����������s��
        // ���������������烊���[�X
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError("Failed to load content catalog.");
        }
        Debug.Log("Addressable�r���h���������܂����B");
    }


    protected async UniTask<string> Duplicate(string orig, string dest)
    {
        Directory.CreateDirectory(dest);
        // �J�^���O���������Ă���f�B���N�g���̒��g�����v���W�F�N�g�̃t�H���_���ɕ���
        DirectoryUtil.CopyParentFolder(orig, dest);

        string newPath = $@"{dest}/{Path.GetFileName(orig)}";
        return newPath;
    }


    string GetOnlyOneFile(string dir, string extention)
    {
        // �w�肵���g���q�̃t�@�C���ꗗ���擾
        string[] files = Directory.GetFiles(dir, $"*.{extention}");
        // �Y���t�@�C�����P�̏ꍇ�̂ݕԂ�
        if (files.Length == 1)
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
                    //Debug.Log($"{b.PrimaryKey} �̓��[�h�ł��Ȃ�����");
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
#endif