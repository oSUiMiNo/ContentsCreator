using Mirror;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class FileClient : NetworkBehaviour
{
    private byte[] receivedFileData;
    private int totalChunks;
    private int chunksReceived = 0;

    private const int chunkSize = 1024 * 1024;  // 1MB���Ƃ̃`�����N�T�C�Y
    private byte[] fileData;


    private void Start()
    {
        InputEventHandler.OnDown_1 += () =>
        {
            if (isClient) RequestFileFromHost();
        };
    }

    public override void OnStartServer()
    {
        // �t�@�C�����o�C�g�f�[�^�ɓǂݍ���
        fileData = File.ReadAllBytes(@$"{Application.dataPath}/Publish/�e�X�g�R���e���c/908b3906d47bc9506a27549be7f6ae55.shiji");
    }

    public void RequestFileFromHost()
    {
        Debug.Log("���N�G�X�g���M");
        // �z�X�g�Ƀt�@�C�������N�G�X�g
        CmdRequestFile();
    }

    [Command]
    private void CmdRequestFile()
    {
        if (isServer) StartCoroutine(SendFileInChunks());
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
    public void RpcSendFileChunk(byte[] chunkData, int chunkIndex, int totalChunks)
    {
        // ����`�����N���Ƀ��������m��
        if (receivedFileData == null)
        {
            this.totalChunks = totalChunks;
            receivedFileData = new byte[totalChunks * chunkData.Length];
        }

        // �f�[�^���󂯎��A�K�؂Ȉʒu�ɏ�������
        System.Array.Copy(chunkData, 0, receivedFileData, chunkIndex * chunkData.Length, chunkData.Length);
        chunksReceived++;

        // ���ׂẴ`�����N���󂯎������t�@�C���������o��
        if (chunksReceived == totalChunks)
        {
            File.WriteAllBytes(@$"{Application.dataPath}/908b3906d47bc9506a27549be7f6ae55.shiji", receivedFileData);
            Debug.Log("File download complete");
        }
    }
}
