using UnityEngine;
using System.IO;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;

public class AssetRenameBytes : AssetPostprocessor
{
    /// <summary>
    /// �������ނ̔C�ӂ̐��̃A�Z�b�g���C���|�[�g�����������Ƃ��ɌĂ΂�鏈��
    /// </summary>
    /// <param name="importedAssets"> �C���|�[�g���ꂽ�A�Z�b�g�̃t�@�C���p�X�B </param>
    /// <param name="deletedAssets"> �폜���ꂽ�A�Z�b�g�̃t�@�C���p�X�B </param>
    /// <param name="movedAssets"> �ړ����ꂽ�A�Z�b�g�̃t�@�C���p�X�B </param>
    /// <param name="movedFromPath"> �ړ����ꂽ�A�Z�b�g�̈ړ��O�̃t�@�C���p�X�B </param>
    static void OnPostprocessAllAssets
        (string[] importedAssets, string[] deletedAssets,
         string[] movedAssets, string[] movedFromPath)
    {
        // �A�Z�b�g���C���|�[�g���ꂽ�ꍇ
        foreach (string asset in importedAssets)
        {
            // �g���q�̂ݎ擾
            string type = Path.GetExtension(asset);

            // vrm�t�@�C�����C���|�[�g������
            if (type == ".vrm")
            {
                VRM_To_TextAsset(asset);
            }

            //// bytes�t�@�C���쐬���ɑ��鏈��
            //if (type == ".bytes")
            //{
            //    // �g���q�Ȃ��̃t�@�C�������擾
            //    string fileNm = Path.GetFileNameWithoutExtension(asset);
            //    // �R�s�[���t�@�C����meta�f�[�^���폜
            //    FileUtil.DeleteFileOrDirectory(asset.Replace(".bytes", ""));
            //    FileUtil.DeleteFileOrDirectory(asset.Replace(".bytes", ".meta"));
            //    Debug.Log(fileNm + "��bytes�g���q�ɕϊ�");
            //    // Editor�ɔ��f�����̒x���̂Ń��t���b�V��
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
            Debug.Log("VRM�t�@�C��������ɍ쐬����܂���: " + savePath_VRMAsBytes);
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.Log("�G���[���������܂���: " + ex.Message);
        }
    }
}
#endif
