using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// jsonファイルにてセーブデータを管理する
public abstract class SaveDataBase<T> where T : SaveDataBase<T>, new()
{
    private static string FileName => typeof(T).Name + ".dat";
    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // 任意のキー（16文字 = 128bit、32文字 = 256bit）
    private static readonly string Key = "my-secret-key-16";    // 16バイト = 128bit
    private static readonly string Iv = "init-vector-1234";    // 16バイト

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(Iv);

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string encryptedText)
    {
        var bytes = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(Iv);

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(bytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        string encrypted = Encrypt(json);

        File.WriteAllText(FilePath, json);
        Debug.Log($"保存完了: {FilePath}");
    }

    public static T Load()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);

            return JsonUtility.FromJson<T>(json);
        }

        Debug.LogWarning($"ファイルが見つかりません: {FilePath}");
        return new T(); // 初期データを返す
    }

    public static void Delete()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log($"削除完了: {FilePath}");
        }
    }
}
