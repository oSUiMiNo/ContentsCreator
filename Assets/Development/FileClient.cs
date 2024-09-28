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

    private const int chunkSize = 1024 * 1024;  // 1MBごとのチャンクサイズ
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
        // ファイルをバイトデータに読み込む
        fileData = File.ReadAllBytes(@$"{Application.dataPath}/Publish/テストコンテンツ/908b3906d47bc9506a27549be7f6ae55.shiji");
    }

    public void RequestFileFromHost()
    {
        Debug.Log("リクエスト送信");
        // ホストにファイルをリクエスト
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

            // クライアントにチャンクを送信
            RpcSendFileChunk(chunkData, i, numberOfChunks);

            yield return null; // フレームごとに送信することで負荷を軽減
        }
    }

    [ClientRpc]
    public void RpcSendFileChunk(byte[] chunkData, int chunkIndex, int totalChunks)
    {
        // 初回チャンク時にメモリを確保
        if (receivedFileData == null)
        {
            this.totalChunks = totalChunks;
            receivedFileData = new byte[totalChunks * chunkData.Length];
        }

        // データを受け取り、適切な位置に書き込む
        System.Array.Copy(chunkData, 0, receivedFileData, chunkIndex * chunkData.Length, chunkData.Length);
        chunksReceived++;

        // すべてのチャンクを受け取ったらファイルを書き出す
        if (chunksReceived == totalChunks)
        {
            File.WriteAllBytes(@$"{Application.dataPath}/908b3906d47bc9506a27549be7f6ae55.shiji", receivedFileData);
            Debug.Log("File download complete");
        }
    }
}
