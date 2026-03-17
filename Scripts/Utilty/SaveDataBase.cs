using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// JSON + AES 暗号化によるセーブデータ基底クラス。
/// 暗号化キーはサブクラスで EncryptionKey / EncryptionIv をオーバーライドして設定すること。
/// </summary>
public abstract class SaveDataBase<T> where T : SaveDataBase<T>, new()
{
    private static string FileName => typeof(T).Name + ".dat";
    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>
    /// 暗号化キー（16 or 32文字）。サブクラスでオーバーライドして固有のキーを設定すること。
    /// </summary>
    protected virtual string EncryptionKey => "please-change-16";

    /// <summary>
    /// 暗号化 IV（16文字固定）。サブクラスでオーバーライドして固有の値を設定すること。
    /// </summary>
    protected virtual string EncryptionIv  => "please-change-iv";

    private static string Encrypt(string plainText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV  = Encoding.UTF8.GetBytes(iv);

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs)) sw.Write(plainText);

        return Convert.ToBase64String(ms.ToArray());
    }

    private static string Decrypt(string encryptedText, string key, string iv)
    {
        var bytes = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV  = Encoding.UTF8.GetBytes(iv);

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(bytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    public void Save()
    {
        string json      = JsonUtility.ToJson(this);
        string encrypted = Encrypt(json, EncryptionKey, EncryptionIv);
        File.WriteAllText(FilePath, encrypted);
        Debug.Log($"保存完了: {FilePath}");
    }

    public static T Load()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning($"ファイルが見つかりません: {FilePath}");
            return new T();
        }

        var temp = new T();
        string raw  = File.ReadAllText(FilePath);
        string json;

        try
        {
            json = Decrypt(raw, temp.EncryptionKey, temp.EncryptionIv);
        }
        catch
        {
            // 旧形式（平文JSON）へのフォールバック
            Debug.LogWarning($"復号に失敗しました。平文として読み込みます: {FilePath}");
            json = raw;
        }

        return JsonUtility.FromJson<T>(json);
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
