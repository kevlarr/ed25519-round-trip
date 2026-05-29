using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Chaos.NaCl;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

var useChaos = args.Contains("--chaos");
var keyPairPath = Path.Combine("..", "keypair.json");
var outputPath = Path.Combine("..", "output.json");

Console.WriteLine($"Generating key pair with {(useChaos ? "Chaos.NaCl" : "BouncyCastle")}...");

byte[] seed;
byte[] publicKeyBytes;

if (useChaos)
{
    seed = new byte[32];
    RandomNumberGenerator.Fill(seed);
    byte[] naclExpandedPrivateKey;
    Ed25519.KeyPairFromSeed(out publicKeyBytes, out naclExpandedPrivateKey, seed);
}
else
{
    var generator = new Ed25519KeyPairGenerator();
    generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
    var keyPair = generator.GenerateKeyPair();
    var bcPrivateKey = (Ed25519PrivateKeyParameters)keyPair.Private;
    var bcPublicKey = (Ed25519PublicKeyParameters)keyPair.Public;
    seed = bcPrivateKey.GetEncoded();
    publicKeyBytes = bcPublicKey.GetEncoded();
}

var keyPairJson = JsonSerializer.Serialize(new
{
    public_key = Convert.ToBase64String(publicKeyBytes),
    private_key = Convert.ToBase64String(seed),
}, new JsonSerializerOptions { WriteIndented = true });

File.WriteAllText(keyPairPath, keyPairJson);
Console.WriteLine($"Key pair written to {Path.GetFullPath(keyPairPath)}");

// Read back the seed as the client would
var stored = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(keyPairPath));
seed = Convert.FromBase64String(stored.GetProperty("private_key").GetString()!);

var message = """{"hello":"world"}""";
var messageBytes = Encoding.UTF8.GetBytes(message);

// Sign with BouncyCastle
var bcSignKey = new Ed25519PrivateKeyParameters(seed, 0);
var bcSigner = new Ed25519Signer();
bcSigner.Init(true, bcSignKey);
bcSigner.BlockUpdate(messageBytes, 0, messageBytes.Length);
var bcSignature = bcSigner.GenerateSignature();

// Sign with Chaos.NaCl
byte[] _, naclExpandedKey;
Ed25519.KeyPairFromSeed(out _, out naclExpandedKey, seed);
var naclSignedMessage = Ed25519.Sign(messageBytes, naclExpandedKey);
var naclSignature = naclSignedMessage.Take(64).ToArray();

// Compare signatures
if (!bcSignature.SequenceEqual(naclSignature))
{
    Console.Error.WriteLine("FAIL: BouncyCastle and Chaos.NaCl produced different signatures");
    Console.Error.WriteLine($"  BouncyCastle: {Convert.ToBase64String(bcSignature)}");
    Console.Error.WriteLine($"  Chaos.NaCl:   {Convert.ToBase64String(naclSignature)}");
    Environment.Exit(1);
}

Console.WriteLine("PASS: BouncyCastle and Chaos.NaCl signatures match");

var outputJson = JsonSerializer.Serialize(new
{
    message,
    signature = Convert.ToBase64String(bcSignature),
}, new JsonSerializerOptions { WriteIndented = true });

File.WriteAllText(outputPath, outputJson);
Console.WriteLine($"Signed output written to {Path.GetFullPath(outputPath)}");
