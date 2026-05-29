# Ed25519 Interop Test

Verifies that BouncyCastle's Ed25519 signing (C#) is interoperable with Go's `crypto/ed25519` verification.

## Files

| File | Description |
|---|---|
| `keypair.json` | Generated key pair. C# writes it; Go reads only the public key from it. |
| `output.json` | Signed message. C# writes the message and signature; Go reads both to verify. |

## Running

Run the full test suite (both keygen paths, both Go verifications) with:
```bash
./run.sh
```

Or run the steps individually:

**Step 1** — generate a key pair, sign a message, write `keypair.json` and `output.json`:
```bash
cd dotnet
dotnet run              # generate key pair with BouncyCastle
dotnet run --chaos      # generate key pair with Chaos.NaCl instead
```

Both paths sign the message with both libraries and assert the signatures are identical before writing output.

**Step 2** — read the public key and verify the signature using `crypto/ed25519`:
```bash
cd go
go run main.go
```

Expected output from the Go step: `PASS: BouncyCastle signature verified by crypto/ed25519`

## Requirements

- .NET 9 SDK
- Go 1.21+
