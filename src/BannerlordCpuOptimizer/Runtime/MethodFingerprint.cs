using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class MethodFingerprint
    {
        internal static string ComputeSha256(MethodBase method)
        {
            MethodBody body = method.GetMethodBody();
            byte[] il = body?.GetILAsByteArray();
            if (il == null)
            {
                return null;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(il);
                var builder = new StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
