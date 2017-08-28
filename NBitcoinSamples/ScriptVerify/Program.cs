// Explanation of how a transaction is signed https://bitcoin.stackexchange.com/questions/3374/how-to-redeem-a-basic-tx.

using System;
using System.Linq;
using System.Threading;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Crypto;
using log4net;

namespace ScriptVerify
{
    class Program
    {
        static ILog logger = log4net.LogManager.GetLogger("default");

        static Network _network = Network.RegTest;
        static string _privateKey = "cTUB1ab9GsxYu9x87MhM17cZehqngGGXSXRijig5uxNYBfrxgcpA";
        static string _scriptPubKey = "024a36f136bd7c114c599efc07bcba8ac32ea6ebaf8e1e209f91316d9bd2eb74e3 OP_CHECKSIG";
        static string _unspentTxId = "3121856b9f99e4855cec9f4df82b913b30bef07dd1f1150097a8d181ff581731";

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            Key spender = Key.Parse(_privateKey);
            Key recipient = new Key();

            Script scriptPubKey = new Script(_scriptPubKey);
            Money amount = Money.Parse("49");
            OutPoint outPoint = new OutPoint(uint256.Parse(_unspentTxId), 0);

            Transaction tx = new Transaction();
            TxIn txIn = new TxIn(outPoint, scriptPubKey);
            tx.AddInput(txIn);
            tx.AddOutput(amount, recipient.ScriptPubKey);

            //logger.Debug(tx.ToHex());
            //logger.Debug(tx.ToString(RawFormat.Satoshi));

            var txHash = tx.GetSignatureHash(new Coin(uint256.Parse(_unspentTxId), 0, Money.Zero, scriptPubKey));
            var txSig = spender.Sign(txHash);
            var manScriptSig = new Script(
                Op.GetPushOp(txSig.ToDER())
                );

            var checkSigResult = spender.PubKey.Verify(txHash, ECDSASignature.FromDER(manScriptSig.ToBytes().Skip(1).ToArray()));
            Console.WriteLine($"Check manual sig result: {checkSigResult}.");

            // Append SigHash method byte to end of script sig array.
            var finalManScriptSig = manScriptSig.ToBytes().ToList();
            finalManScriptSig.Add(Convert.ToByte(SigHash.All));
            finalManScriptSig[0] += 1;

            txIn.ScriptSig = new Script(finalManScriptSig.ToArray(), false);

            // OR use the transaction sign method and generate the script signature.
            //tx.Sign(spender, false);

            Script scriptSig = tx.Inputs.First().ScriptSig;
           logger.Debug($"scriptSig: {Encoders.Hex.EncodeData(scriptSig.ToBytes())}");
           
            // Verify transaction signature script.
            uint256 sigHash = Script.SignatureHash(scriptPubKey, tx.Outputs.Transaction, 0, SigHash.All, amount, HashVersion.Original, null);
            var verifyResult = spender.PubKey.Verify(sigHash, ECDSASignature.FromDER(scriptSig.ToBytes().Skip(1).ToArray()));
            Console.WriteLine($"Verify signature script result {verifyResult}.");

            logger.Debug(tx.ToString(RawFormat.Satoshi));

            if (verifyResult == true)
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
