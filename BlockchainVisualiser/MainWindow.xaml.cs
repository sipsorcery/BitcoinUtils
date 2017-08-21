using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using log4net;
using NBitcoin;
using NBitcoin.Protocol;

namespace BlockchainVisualiser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ILog logger = log4net.LogManager.GetLogger("default");
        private Network _network = Network.RegTest;
        private Node _node;
        private uint256 _requestedBlockHash;
        private Newtonsoft.Json.JsonSerializer _jsonSerializer = new Newtonsoft.Json.JsonSerializer();

        public MainWindowViewModel ViewModel
        {
            get
            {
                return root.DataContext as MainWindowViewModel;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            root.DataContext = new MainWindowViewModel();
        }

        private async Task<Node> InitialiseNode()
        {
            if (_node == null)
            {
                await Task.Run(() =>
                {
                    _node = Node.ConnectToLocal(_network);
                    _node.VersionHandshake();
                });

                _node.MessageReceived += Node_MessageReceived;
            }

            return _node;
        }

        private void Node_MessageReceived(Node node, IncomingMessage message)
        {
            switch (message.Message.Payload)
            {
                case BlockPayload block:

                    if (block.Object.Header.GetHash() == _requestedBlockHash)
                    {
                        App.Current.Dispatcher.Invoke(delegate {
                            ViewModel.CurrentBlock = block.Object;
                            ViewModel.CurrentTransaction = block.Object.Transactions.FirstOrDefault()?.ToString(RawFormat.BlockExplorer);
                            //foreach (var tx in block.Object.Transactions)
                            //{
                            //    ViewModel.CurrentBlock += tx.ToString(RawFormat.BlockExplorer, _network);
                            //}
                        });
                    }
                    break;

                case HeadersPayload hdr:
                    break;

                case InvPayload inv:
                    logger.DebugFormat("Inventory items {0}, first type {1}.", inv.Count(), inv.First().Type);
                    break;

                //foreach (var invPayload in inv.AsEnumerable())
                //{

                //}

                //break;

                case MerkleBlockPayload merkleBlk:
                    //foreach (var tx in merkleBlk.Object.PartialMerkleTree.GetMatchedTransactions())
                    //{
                    //    logger.DebugFormat("Matched merkle block TX ID {0}.", tx);
                    //    txs.Add(tx);
                    //}

                    //if (searchBlocksIndex < searchBlocks.Count())
                    //{
                    //    var dp = new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, searchBlocks[searchBlocksIndex++].HashBlock));
                    //    node.SendMessage(dp);
                    //}
                    //else
                    //{
                    //    searchCompleteSignal.Set();
                    //}

                    break;

                case TxPayload tx:
                    logger.DebugFormat("TX ID {0}.", tx.Object.GetHash());
                    break;
            }
        }

        private async void LoadBlockButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            string searchText = _blockSearchText.Text;

            if (!String.IsNullOrEmpty(searchText))
            {
                _requestedBlockHash = uint256.Parse(searchText);

                var node = await InitialiseNode();

                var dp = new GetDataPayload(new InventoryVector(InventoryType.MSG_BLOCK, _requestedBlockHash));
                node.SendMessageAsync(dp);
            }

            (sender as Button).IsEnabled = true;
        }
    }
}
