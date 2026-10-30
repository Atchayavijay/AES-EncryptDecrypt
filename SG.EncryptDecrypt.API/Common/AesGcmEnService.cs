using System;
using System.Security.Cryptography;
using System.Text;
namespace SG.LoyaltyApps.API.Common
{
    // Format: v1.<base64_nonce>.<base64_tag>.<base64_ciphertext>
    public sealed class AesGcmEnService : IEnService
    {
        
        private const int _s = 32;      
        private const string _base = "";
        private const int NonceSizeBytes = 12; 
        private const int TagSizeBytes = 16;  

        private readonly byte[] _key;

        public AesGcmEnService() : this(_base)
        {
        }

        public AesGcmEnService(string _bk)
        {
            if (string.IsNullOrWhiteSpace(_bk))
                throw new ArgumentException("error occured.");

            _key = Convert.FromBase64String(_bk);

            if (_key.Length != _s)
                throw new ArgumentException("error occured.");
        }

        public string ecr(string nt)
        {
            if (string.IsNullOrEmpty(nt))
                return nt;

            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] ptb = Encoding.UTF8.GetBytes(nt);
            byte[] ct = new byte[ptb.Length];
            byte[] tag = new byte[TagSizeBytes];

            using var aes = new AesGcm(_key, TagSizeBytes);
            aes.Encrypt(nonce, ptb, ct, tag);

            return $"v1.{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(tag)}.{Convert.ToBase64String(ct)}";
        }

        public string dcr(string et)
        {
            if (string.IsNullOrEmpty(et))
                return et;

            string[] parts = et.Split('.');
            if (parts.Length != 4 || parts[0] != "v1")
                throw new CryptographicException("error occured.");

            byte[] nonce, tag, _ct;
            try
            {
                nonce = Convert.FromBase64String(parts[1]);
                tag = Convert.FromBase64String(parts[2]);
                _ct = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("error occured", ex);
            }

            if (nonce.Length != NonceSizeBytes || tag.Length != TagSizeBytes)
                throw new CryptographicException("error occured.");

            byte[] pt = new byte[_ct.Length];

            using var aes = new AesGcm(_key, TagSizeBytes);
            aes.Decrypt(nonce, _ct, tag, pt);

            return Encoding.UTF8.GetString(pt);
        }
    }
}
