using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace YFan.Utils
{
    /// <summary>
    /// 二进制数据处理工具
    /// * 提供 对象/字符串 <-> 二进制 的转换
    /// * 提供 AES 加密/解密
    /// * 提供 GZip 压缩/解压
    /// * 提供 MD5 哈希计算
    /// * 提供 二进制文件读写
    /// </summary>
    public static class BinaryUtil
    {
        #region 转换 (Convert)

        /// <summary>
        /// 对象 -> 二进制 (基于 JSON 序列化)
        /// </summary>
        public static byte[] ToBytes(object obj)
        {
            if (obj == null) return null;
            string json = JSONUtil.ToJson(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 字符串 -> 二进制
        /// </summary>
        public static byte[] ToBytes(string str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// 二进制 -> 对象
        /// </summary>
        public static T ToObject<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return default;
            string json = Encoding.UTF8.GetString(bytes);
            return JSONUtil.FromJson<T>(json);
        }

        /// <summary>
        /// 二进制 -> 字符串
        /// </summary>
        public static string ToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            return Encoding.UTF8.GetString(bytes);
        }

        #endregion

        #region 加密/解密 (AES)

        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="key">密钥 (任意字符串，内部会自动Hash处理)</param>
        public static byte[] Encrypt(byte[] data, string key = ConfigKeys.BinarySecretKey)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                byte[] keyArray = GetMD5Hash(key);
                byte[] ivArray = GetMD5Hash(ConfigKeys.BinaryIV);

                using (RijndaelManaged rDel = new RijndaelManaged())
                {
                    rDel.Key = keyArray;
                    rDel.IV = ivArray;
                    rDel.Mode = CipherMode.CBC;
                    rDel.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform cTransform = rDel.CreateEncryptor())
                    {
                        return cTransform.TransformFinalBlock(data, 0, data.Length);
                    }
                }
            }
            catch (Exception e)
            {
                YLog.Error($"加密失败: {e.Message}", "BinaryUtil");
                return null;
            }
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        public static byte[] Decrypt(byte[] data, string key = ConfigKeys.BinarySecretKey)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                byte[] keyArray = GetMD5Hash(key);
                byte[] ivArray = GetMD5Hash(ConfigKeys.BinaryIV);

                using (RijndaelManaged rDel = new RijndaelManaged())
                {
                    rDel.Key = keyArray;
                    rDel.IV = ivArray;
                    rDel.Mode = CipherMode.CBC;
                    rDel.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform cTransform = rDel.CreateDecryptor())
                    {
                        return cTransform.TransformFinalBlock(data, 0, data.Length);
                    }
                }
            }
            catch (Exception e)
            {
                YLog.Error($"解密失败: {e.Message} (密钥错误或数据损坏)", "BinaryUtil");
                return null;
            }
        }

        #endregion

        #region 压缩/解压 (GZip)

        /// <summary>
        /// GZip 压缩 (大幅减小体积，适用于网络包或大存档)
        /// </summary>
        public static byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                    {
                        gzip.Write(data, 0, data.Length);
                    }
                    return output.ToArray();
                }
            }
            catch (Exception e)
            {
                YLog.Error($"压缩失败: {e.Message}", "BinaryUtil");
                return null;
            }
        }

        /// <summary>
        /// GZip 解压
        /// </summary>
        public static byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                using (MemoryStream input = new MemoryStream(data))
                using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                using (MemoryStream output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch (Exception e)
            {
                YLog.Error($"解压失败: {e.Message}", "BinaryUtil");
                return null;
            }
        }

        #endregion

        #region 文件 IO

        /// <summary>
        /// 写入二进制文件
        /// </summary>
        public static bool WriteToFile(string path, byte[] data)
        {
            try
            {
                // 自动创建目录
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception e)
            {
                YLog.Error($"写入文件失败: {path} \n{e.Message}", "BinaryUtil");
                return false;
            }
        }

        /// <summary>
        /// 读取二进制文件
        /// </summary>
        public static byte[] ReadFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                YLog.Error($"读取文件失败: {path} \n{e.Message}", "BinaryUtil");
                return null;
            }
        }

        #endregion

        #region 辅助工具 (Hash)

        /// <summary>
        /// 计算字符串的 MD5 哈希 (16字节)
        /// 常用于将任意长度的密码转为 AES 密钥
        /// </summary>
        public static byte[] GetMD5Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return new byte[16];
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        /// <summary>
        /// 计算文件的 MD5 (用于校验文件一致性/热更对比)
        /// </summary>
        public static string GetFileMD5(string filePath)
        {
            if (!File.Exists(filePath)) return "";
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                YLog.Error($"计算MD5失败: {e.Message}", "BinaryUtil");
                return "";
            }
        }

        #endregion
    }
}
