using Mirror;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FileHost : NetworkBehaviour
{
    //private string filePath = "path/to/your/file.txt";  // 実際のファイルパスを指定
    private const int chunkSize = 1024 * 1024;  // 1MBごとのチャンクサイズ
    private byte[] fileData;

    public override void OnStartServer()
    {
        // ファイルをバイトデータに読み込む
        fileData = File.ReadAllBytes(@$"{Application.dataPath}/Publish/テストコンテンツ/908b3906d47bc9506a27549be7f6ae55.shiji");
    }

    [Command] // クライアントからのリクエストを受け取る
    public void CmdRequestFile()
    {
        Debug.Log("リクエスト受診");
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

            // クライアントにチャンクを送信
            RpcSendFileChunk(chunkData, i, numberOfChunks);

            yield return null; // フレームごとに送信することで負荷を軽減
        }
    }

    [ClientRpc]
    private void RpcSendFileChunk(byte[] chunkData, int chunkIndex, int totalChunks)
    {
        // クライアント側で処理される
        // クライアントは後でデータを受け取るため、この部分はクライアント側の処理で対応
    }
}
