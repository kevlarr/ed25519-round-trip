package main

import (
	"crypto/ed25519"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"os"
)

type keyPair struct {
	PublicKey string `json:"public_key"`
}

type output struct {
	Message   string `json:"message"`
	Signature string `json:"signature"`
}

func main() {
	keyPairData, err := os.ReadFile("../keypair.json")
	if err != nil {
		fmt.Fprintf(os.Stderr, "error reading keypair.json: %v\n", err)
		os.Exit(1)
	}

	var kp keyPair
	if err := json.Unmarshal(keyPairData, &kp); err != nil {
		fmt.Fprintf(os.Stderr, "error parsing keypair.json: %v\n", err)
		os.Exit(1)
	}

	publicKey, err := base64.StdEncoding.DecodeString(kp.PublicKey)
	if err != nil || len(publicKey) != ed25519.PublicKeySize {
		fmt.Fprintf(os.Stderr, "invalid public key\n")
		os.Exit(1)
	}

	outputData, err := os.ReadFile("../output.json")
	if err != nil {
		fmt.Fprintf(os.Stderr, "error reading output.json: %v\n", err)
		os.Exit(1)
	}

	var o output
	if err := json.Unmarshal(outputData, &o); err != nil {
		fmt.Fprintf(os.Stderr, "error parsing output.json: %v\n", err)
		os.Exit(1)
	}

	signature, err := base64.StdEncoding.DecodeString(o.Signature)
	if err != nil || len(signature) != ed25519.SignatureSize {
		fmt.Fprintf(os.Stderr, "invalid signature\n")
		os.Exit(1)
	}

	if ed25519.Verify(ed25519.PublicKey(publicKey), []byte(o.Message), signature) {
		fmt.Println("PASS: BouncyCastle signature verified by crypto/ed25519")
	} else {
		fmt.Fprintln(os.Stderr, "FAIL: signature verification failed")
		os.Exit(1)
	}
}
