// ============================================================================
// FileName: Program.cs
//
// Description:
// A minimal BitCoin client that submits transactions to a local bitcoin full node running in
// regression test mdoe and uses a combination of extracting data with the bitcoin-cli command line
// utility and NBitcoin to spend some bitcoins.
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
// 17 Aug 2017  Aaron Clauson          Created.
//
// License: 
// Public Domain
// =============================================================================

using System;
using System.Threading;
using NBitcoin;
using NBitcoin.Protocol;
using log4net;

namespace ValidateTransactionTest
{
    class Program
    {
        static ILog logger = log4net.LogManager.GetLogger("default");
        static Network _network = Network.RegTest;

        static string _unspentTxId = "5d1db816efc865ab33eb8d5c9f0238501dfd849fc67cc941565236b36e43b234";    // Need to get this from bitcoin-cli (see Step 4 above).
        static string _unspentScriptPubKey = "03c1a1a614c8549373b2ec35f586aa8b33a3bf5ac3e0a1b8cf27e650bdb5a126f0 OP_CHECKSIG"; // Need to get this from bitcoin-cli (see Step 4 above).
        static string _sendFromPrivateKey = "cQYdUpoeJZP7FmxUeiaKSLPo9eHsDAYbWs17DgY44yHX2sATK2Cw";         // Need to get this from bitcoin-cli (see Step 5 above).
        static string _receiveToPrivateKey = "cR7X4Nd5WqA5mNwgX67th4Jo3K9vTTm28w8njLL9JT8hHPdbstL8";        // This is an arbitrary key that is used to send some coins to.

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            // Set up the private keys and addresses for the sender and receiver.
            Key sendFromPrivKey = Key.Parse(_sendFromPrivateKey, _network);
            BitcoinPubKeyAddress sendFromAddr = sendFromPrivKey.PubKey.GetAddress(_network);

            Key receiveToPrivKey = Key.Parse(_receiveToPrivateKey, _network);
            BitcoinPubKeyAddress receiveToAddr = receiveToPrivKey.PubKey.GetAddress(_network);

            logger.DebugFormat("Sending from {0} to {1}.", sendFromAddr, receiveToAddr);

            logger.Debug(sendFromPrivKey.ScriptPubKey);
            logger.Debug(sendFromPrivKey.PubKey);

            // Create the transaction to spend the bitcoin.
            OutPoint spending = new OutPoint(uint256.Parse(_unspentTxId), 0);
            Script spendScriptPubKey = new Script(_unspentScriptPubKey);

            var spendTx = new Transaction();
            spendTx.Inputs.Add(new TxIn(spending, spendScriptPubKey));
            spendTx.Outputs.Add(new TxOut(Money.Parse("49"), receiveToAddr.ScriptPubKey));  

            spendTx.Sign(sendFromPrivKey, false);

            logger.Debug(spendTx.ToString(RawFormat.BlockExplorer));

            // Send the transaction to the local bitcoin node.
            using (var node = Node.ConnectToLocal(_network))
            {
                node.VersionHandshake();
                node.SendMessage(new InvPayload(InventoryType.MSG_TX, spendTx.GetHash()));
                node.SendMessage(new TxPayload(spendTx));
                Thread.Sleep(500);
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
