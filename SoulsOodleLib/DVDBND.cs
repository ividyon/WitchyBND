using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using SoulsFormats;

namespace SoulsOodleLib;

public static class DVDBND
{
    private const uint PRIME = 37;
    private const ulong PRIME64 = 0x85ul;

    private static string data0Key = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEA9Rju2whruXDVQZpfylVEPeNxm7XgMHcDyaaRUIpXQE0qEo+6Y36L
P0xpFvL0H0kKxHwpuISsdgrnMHJ/yj4S61MWzhO8y4BQbw/zJehhDSRCecFJmFBz
3I2JC5FCjoK+82xd9xM5XXdfsdBzRiSghuIHL4qk2WZ/0f/nK5VygeWXn/oLeYBL
jX1S8wSSASza64JXjt0bP/i6mpV2SLZqKRxo7x2bIQrR1yHNekSF2jBhZIgcbtMB
xjCywn+7p954wjcfjxB5VWaZ4hGbKhi1bhYPccht4XnGhcUTWO3NmJWslwccjQ4k
sutLq3uRjLMM0IeTkQO6Pv8/R7UNFtdCWwIERzH8IQ==
-----END RSA PUBLIC KEY-----";

    /// <summary>
    ///     Decrypts a file with a provided decryption key.
    /// </summary>
    /// <param name="filePath">An encrypted file</param>
    /// <param name="key">The RSA key in PEM format</param>
    /// <exception cref="ArgumentNullException">When the argument filePath is null</exception>
    /// <exception cref="ArgumentNullException">When the argument keyPath is null</exception>
    /// <returns>A memory stream with the decrypted file</returns>
    private static MemoryStream DecryptRsa(string filePath, string key)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        AsymmetricKeyParameter keyParameter = GetKeyOrDefault(key);
        RsaEngine engine = new RsaEngine();
        engine.Init(false, keyParameter);

        MemoryStream outputStream = new MemoryStream();
        using (FileStream inputStream = File.OpenRead(filePath))
        {
            int inputBlockSize = engine.GetInputBlockSize();
            int outputBlockSize = engine.GetOutputBlockSize();
            byte[] inputBlock = new byte[inputBlockSize];
            while (inputStream.Read(inputBlock, 0, inputBlock.Length) > 0)
            {
                byte[] outputBlock = engine.ProcessBlock(inputBlock, 0, inputBlockSize);

                int requiredPadding = outputBlockSize - outputBlock.Length;
                if (requiredPadding > 0)
                {
                    byte[] paddedOutputBlock = new byte[outputBlockSize];
                    outputBlock.CopyTo(paddedOutputBlock, requiredPadding);
                    outputBlock = paddedOutputBlock;
                }

                outputStream.Write(outputBlock, 0, outputBlock.Length);
            }
        }

        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    private static ulong ComputeHash(string path, BHD5.Game game)
    {
        string hashable = path.Trim().Replace('\\', '/').ToLowerInvariant();
        if (!hashable.StartsWith("/"))
            hashable = '/' + hashable;
        return game >= BHD5.Game.EldenRing
            ? hashable.Aggregate(0ul, (i, c) => i * PRIME64 + c)
            : hashable.Aggregate(0u, (i, c) => i * PRIME + c);
    }

    public static AsymmetricKeyParameter GetKeyOrDefault(string key)
    {
        try
        {
            PemReader pemReader = new PemReader(new StringReader(key));
            return (AsymmetricKeyParameter)pemReader.ReadObject();
        }
        catch
        {
            return null;
        }
    }
    public static bool CreateAssets(AssetLocator.Game game, string modPath, List<(string, string)> paths, Action<string> writeLineFunction, bool useFolderPicker, bool copyToAppFolder)
    {
        var modPaths = paths.Select(a => Path.Combine(modPath, a.Item2)).ToList();

        if (modPaths.All(p => File.Exists(p)))
        {
            writeLineFunction("All files already accounted for, no need to unpack.");
            return true;
        }

        string? gamePath = AssetLocator.GetGamePath(new []{ game }, writeLineFunction, useFolderPicker);

        if (gamePath == null)
        {
            writeLineFunction("The Game folder could not be located.");
            return false;
        }

        var gameHeaderPath = Path.Combine(gamePath, "Data0.bhd");
        var gameDataPath = Path.Combine(gamePath, "Data0.bdt");

        var oodleResult = Oodle.GrabOodle(writeLineFunction, useFolderPicker, copyToAppFolder, gamePath);
        if (oodleResult == IntPtr.Zero)
        {
            return false;
        }

        BHD5 bhd = null;
        try
        {
            bool encrypted;
            using (FileStream fs = File.OpenRead(gameHeaderPath))
            {
                var magic = new byte[4];
                fs.Read(magic, 0, 4);
                encrypted = Encoding.ASCII.GetString(magic) != "BHD5";
            }

            if (encrypted)
            {
                using MemoryStream bhdStream = DecryptRsa(gameHeaderPath, data0Key);
                bhd = BHD5.Read(bhdStream, BHD5.Game.EldenRing);
            }
            else
            {
                using FileStream bhdStream = File.OpenRead(gameHeaderPath);
                bhd = BHD5.Read(bhdStream, BHD5.Game.EldenRing);
            }
        }
        catch (OverflowException ex)
        {
            Oodle.KillOodle();
            writeLineFunction($"Failed to open BHD:\n{gameHeaderPath}\n\n{ex}");
            return false;
        }

        if (bhd == null)
        {
            Oodle.KillOodle();
            writeLineFunction("Could not open the BHD file for reading.");
            return false;
        }

        using FileStream bdtStream = File.OpenRead(gameDataPath);
        var count = 0;
        var countToMeet = 0;
        Dictionary<ulong, string> hashes = new();
        foreach ((string, string) tuple in paths)
        {
            var path = tuple.Item2;
            countToMeet += 1;
            hashes.TryAdd(ComputeHash(path, BHD5.Game.EldenRing), path);
        }

        foreach (BHD5.Bucket bucket in bhd.Buckets)
        {
            foreach (BHD5.FileHeader header in bucket)
            {
                    if (hashes.ContainsKey(header.FileNameHash))
                    {
                        var modFilePath = Path.Combine(modPath, hashes[header.FileNameHash]);
                        count++;
                        Directory.CreateDirectory(Path.GetDirectoryName(modFilePath));
                        File.WriteAllBytes(modFilePath, header.ReadFile(bdtStream));
                    }

                if (count >= countToMeet)
                    break;
            }
        }

        Oodle.KillOodle();
        writeLineFunction($"Successfully unpacked {count} files to mod folder.");
        return true;
    }
}