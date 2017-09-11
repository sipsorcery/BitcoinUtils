// ============================================================================
// FileName: Program.cs
//
// Description:
// A small console application to test the mechanism that BitCoin uses to generate the scipt signature
// (scriptSig) for an input transaction.
// 
// Most memorable concepts learnt from this program were:
// - As far as signing and verification go each transaction input is an isolated entity,
// - A transaction input is initialised with a ScriptPubKey, which is the instruction the creator
//  of the transaction being spent created, that gets replaced with a ScriptSignature,
//  which is how the spender of the input proves they are entitled to spend the previous transaction,
// - To generate a scriptSig the fields used are:
//   - Previous transaction ID,
//   - The previous transaction scriptPubKey that is being spent (there can be multiple outputs so the nIndex is used to specify which one),
// - When a peer is verifying a transaction there are two scripts that get processed:
//   - Firstly the scriptSig for the input transaction is pushed onto the stack,
//   - Secondly the scriptPubKey from the previous transaction input is invoked and needs to return true for the tx to be accepted into the 
//     mempool or included in a block by a miner.
//
// Note that it would be possible to get all the information needed using NBitcoin without needing bitcoin-cli.
// Using the command line utility is a very good learning experience. 
//
// Dependencies:
// The program relies on NBitcoin (https://github.com/MetacoSA/NBitcoin) for the 
// underlying BitCoin primitives.
//
// Step 1: The command line used for the local bitcoin full node in regression test mode (https://bitcoin.org/en/developer-examples#regtest-mode):
// "C:\Program Files\Bitcoin\daemon\bitcoind" printtoconsole -datadir=f:\temp\bitcoind -server -regtest -debug=1
//
// Step 2: The command line used to request the bitcoin daemon to generate a 101 height blockchain:
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest generate 101
//
// Step 3: If the bitcoin server node was initialised with an empty blockchains the getbalance command should now display a 50.00000000 which
// represents the coin base amount from the first block following the genesis block.
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest getbalance
//
// Step 4: To find the transactions available for spending.
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest listunspent
// ["C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest getrawtransaction <txid> true]
//
// Step 5: To get the private key to sign a spend transaction (MAKE SURE -regtest IS SPECIFIED OTHERWISE YOU COULD EXPORT YOUR LIVE PRIVATE KEY AND POTENTIALLY LOSE $$$).
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest dumpprivkey <address>
//
// Step 6: After sending a transaction check whether it was accepted as valid and added to the mempool.
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest getrawmempool
//
// Step 7: If the transaction is successfully validated and accepted into the mempool then the next step is to generate a block that includes it.
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest generate 1
//
// Step 8: The address that the coins were sent to can then be checked to verify that the coins were received (not you can't use getbalance unless the send to address's
// private key is imported into the wallet).
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest importaddress mssuKhM1CMDgcCm3LyGunA1o6129FnkHyk rescan
// "C:\Program Files\Bitcoin\daemon\bitcoin-cli" -datadir=f:\temp\bitcoind -regtest getreceivedbyaddress mssuKhM1CMDgcCm3LyGunA1o6129FnkHyk
//
// Author(s):
// Aaron Clauson (https://github.com/sipsorcery)
//
// History:
// 29 Aug 2017  Aaron Clauson          Created.
//
// License: 
// Public Domain
// =============================================================================

using System;
using System.Linq;
using System.Threading;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Crypto;
using log4net;

namespace ScriptSigGnerate
{
    class Program
    {
        static ILog logger = log4net.LogManager.GetLogger("default");

        static Network _network = Network.RegTest;
        static string _privateKey = "cTUB1ab9GsxYu9x87MhM17cZehqngGGXSXRijig5uxNYBfrxgcpA";
        static string _scriptPubKey = "024a36f136bd7c114c599efc07bcba8ac32ea6ebaf8e1e209f91316d9bd2eb74e3 OP_CHECKSIG";
        static string _unspentTxId = "0478a7b4f260df599bfb114c8043e2d8925e16b622d12294a4936625ab8ba470";

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            Key spender = Key.Parse(_privateKey);
            Key recipient = new Key();

            Script scriptPubKey = new Script(_scriptPubKey);
            Money amount = Money.Parse("48");
            OutPoint outPoint = new OutPoint(uint256.Parse(_unspentTxId), 0);

            Transaction tx = new Transaction();
            TxIn txIn = new TxIn(outPoint, scriptPubKey);
            txIn.Sequence = 1;  // This field can be incremented to submit the same transaction multiple times. The amount will also need to be reduced to increase the mining fee.
            tx.AddInput(txIn);
            tx.AddOutput(amount, recipient.ScriptPubKey);

            //logger.Debug(tx.ToHex());
            //logger.Debug(tx.ToString(RawFormat.Satoshi));

            #region Construct transaction signature manually (proper way is tx.sign). 

            var txHash = tx.GetSignatureHash(new Coin(uint256.Parse(_unspentTxId), 0, Money.Zero, scriptPubKey));
            var txSig = spender.Sign(txHash);
            var scriptSig = new Script(
                Op.GetPushOp(txSig.ToDER())
                );

            // Append SigHash method byte to end of script sig array. 
            var rawScriptSig = scriptSig.ToBytes().ToList();
            rawScriptSig.Add((byte)SigHash.All);
            rawScriptSig[0] += 1;   // Update the script length field by one.

            scriptSig = new Script(rawScriptSig.ToArray(), false);
            logger.Debug($"scriptSig: {Encoders.Hex.EncodeData(scriptSig.ToBytes())}");

            txIn.ScriptSig = scriptSig;

            #endregion

            // Verify transaction signature script, this doesn't modify the tx, it's double checking the signature field in scriptSig 
            // is a signed version of yhr tx hash where the signing key was the private key matching the spender's public key.
            // Note the way the signature is extracted from the scriptSig will depend on the type of scriptPubKey it was generated for,
            // i.e. the scriptSig format differs for P2PK, P2PKH, P2PKHS etc.
            var scriptSigParams = PayToPubkeyTemplate.Instance.ExtractScriptSigParameters(scriptSig);
            var verifyTxHashSignature = spender.PubKey.Verify(txHash, scriptSigParams.Signature);
            //var verifyCoinHashSignature = spender.PubKey.Verify(coinHash, ECDSASignature.FromDER(scriptSig.ToBytes().Skip(1).ToArray()));
            Console.WriteLine($"Valid signature result {verifyTxHashSignature}.");

            // The verify script checks that the combined script signature (scriptSig) and original script (scriptPubKey) from the transaction
            // being spent evaluate to true. This is the step that Bitcoin peers and miners need to perform in order to determine whether
            // a previous unpsent transaction can be spent by a new transaction.
            var scriptVerifyResult = Script.VerifyScript(scriptPubKey, tx, 0, Money.Zero, out var scriptError);
            Console.WriteLine($"Script verify result {scriptVerifyResult}, script error {scriptError}.");

            if (scriptVerifyResult == true)
            {
                // Send the transaction to the local bitcoin node.
                using (var node = Node.ConnectToLocal(_network))
                {
                    node.VersionHandshake();
                    node.SendMessage(new InvPayload(InventoryType.MSG_TX, tx.GetHash()));
                    node.SendMessage(new TxPayload(tx));
                    Thread.Sleep(500);
                }
            }

            Console.WriteLine("Press q to quit...");

            while (true)
            {
                var keyPress = Console.ReadKey();
                if (keyPress.KeyChar == 'q')
                {
                    break;
                }
            }

            Console.WriteLine("Exiting...");
        }
    }
}
