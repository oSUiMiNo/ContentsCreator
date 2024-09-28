using Mirror;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FileHost : NetworkBehaviour
{
    //private string filePath = "path/to/your/file.txt";  // ���ۂ̃t�@�C���p�X���w��
    private const int chunkSize = 1024 * 1024;  // 1MB���Ƃ̃`�����N�T�C�Y
    private byte[] fileData;

    public override void OnStartServer()
    {
        // �t�@�C�����o�C�g�f�[�^�ɓǂݍ���
        fileData = File.ReadAllBytes(@$"{Application.dataPath}/Publish/�e�X�g�R���e���c/908b3906d47bc9506a27549be7f6ae55.shiji");
    }

    [Command] // �N���C�A���g����̃��N�G�X�g���󂯎��
    public void CmdRequestFile()
    {
        Debug.Log("���N�G�X�g��f");
        StartCoroutine(SendFileInChunks());
    }

    private IEnumerator SendFileInChunks()
    {
        int totalSize = fileData.Length;
        int numberOfChunks = Mathf.CeilToInt((float)totalSize / chunkSize);

        for (int i = 0; i < numberOfChunks; i++)
        {
            int currentChunkSize = Mathf.Min(chunkSize, totalSize - (i * chunkSize));
            byte[] chunkData = new byte[currentChunkSize];
            System.Array.Copy(fileData, i * chunkSize, chunkData, 0, currentChunkSize);

            // �N���C�A���g�Ƀ`�����N�𑗐M
            RpcSendFileChunk(chunkData, i, numberOfChunks);

            yield return null; // �t���[�����Ƃɑ��M���邱�Ƃŕ��ׂ��y��
        }
    }

    [ClientRpc]
    private void RpcSendFileChunk(byte[] chunkData, int chunkIndex, int totalChunks)
    {
        // �N���C�A���g���ŏ��������
        // �N���C�A���g�͌�Ńf�[�^���󂯎�邽�߁A���̕����̓N���C�A���g���̏����őΉ�
    }
}
