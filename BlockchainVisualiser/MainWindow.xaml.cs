using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private const int WAIT_RESPONSE_TIMEOUT_MILLISECONDS = 3000;

        private ILog logger = log4net.LogManager.GetLogger("default");
        private Network _network = Network.RegTest;
        private Node _node;
        private uint256 _requestedBlockHash;
        private Newtonsoft.Json.JsonSerializer _jsonSerializer = new Newtonsoft.Json.JsonSerializer();
        private ManualResetEventSlim _waitForGetDataResponseSignal = new ManualResetEventSlim();        // If a getdata request is made for a non-existent block no response will be received.
        private ManualResetEventSlim _waitForGeHeadersResponseSignal = new ManualResetEventSlim();
        private CancellationTokenSource _cancellationTokenSource;

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

            _cancellationTokenSource = new CancellationTokenSource();

            root.DataContext = new MainWindowViewModel();

            ViewModel.Status = MainWindowViewModel.STATUS_READY_TEXT;
            ViewModel.DisplayFormat_BlockExplorer = true;
            //ViewModel.BlockChainHeaders = new ConcurrentChain(_network);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_node != null)
            {
                _node.MessageReceived -= Node_MessageReceived;
                _node.DisconnectAsync();
            }
        }

        private async Task<Node> InitialiseNode()
        {
            if (_node == null)
            {
                try
                {
#pragma warning disable CS1998
                    await Task.Run(async () =>
                    {
                        _node = Node.ConnectToLocal(_network);
                        _node.VersionHandshake();
                    });
#pragma warning restore

                    if (_node != null)
                    {
                        _node.MessageReceived += Node_MessageReceived;
                    }
                }
                catch(Exception excp)
                {
                    logger.Error($"Exception InitialisedNode. {excp}");

                    // No response to get data request.
                    App.Current.Dispatcher.Invoke(delegate
                    {
                        ViewModel.Status = $"Node connection error. {excp.Message}";
                    });
                }
            }

            return _node;
        }

        private void Node_MessageReceived(Node node, IncomingMessage message)
        {
            switch (message.Message.Payload)
            {
                case BlockPayload block:

                    _waitForGetDataResponseSignal.Set();

                    if (block.Object is null)
                    {
                        App.Current.Dispatcher.Invoke(delegate
                        {
                            ViewModel.Status = "Block not found.";
                        });
                    }
                    else if (block.Object.Header.GetHash() == _requestedBlockHash)
                    {
                        App.Current.Dispatcher.Invoke(delegate
                        {
                            ViewModel.CurrentBlock = block.Object;
                            ViewModel.CurrentTransaction = block.Object.Transactions.FirstOrDefault()?.ToString(ViewModel.BlockDisplayFormat);
                            ViewModel.CurrentTransactionRaw = block.Object.Transactions.FirstOrDefault()?.ToHex();
                            ViewModel.Status = MainWindowViewModel.STATUS_READY_TEXT;
                        });
                    }
                    break;


                case HeadersPayload hdr:

                    _waitForGeHeadersResponseSignal.Set();

                    if (hdr.Headers != null && hdr.Headers.Count > 0)
                    {

                        //logger.DebugFormat("Received {0} blocks start {1} to {2} height {3}.", hdr.Headers.Count, hdr.Headers.First().BlockTime, hdr.Headers.Last().BlockTime, ViewModel.BlockChainHeaders.Height);

                        //scanLocation.Blocks.Clear();
                        //scanLocation.Blocks.Add(hdr.Headers.Last().GetHash());

                        App.Current.Dispatcher.Invoke(delegate
                        {
                            ViewModel.Status = $"Received {hdr.Headers.Count} block headers.";

                            //var tip = ViewModel.BlockChainHeaders.Tip;
                            //foreach (var header in hdr.Headers)
                            //{
                            //    var prev = tip.FindAncestorOrSelf(header.HashPrevBlock);
                            //    if (prev == null)
                            //    {
                            //        break;
                            //    }
                            //    tip = new ChainedBlock(header, header.GetHash(), prev);
                            //    ViewModel.BlockChainHeaders.SetTip(tip);

                            //    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            //}    

                            foreach (var header in hdr.Headers)
                            {
                                ViewModel.BlockChainHeaders.Add(new DummyHeader() { BlockHash = header.GetHash().ToString() });
                            }
                        });
                        //var getHeadersPayload = new GetHeadersPayload(scanLocation);
                        //node.SendMessageAsync(getHeadersPayload);
                    }
                    else
                    {
                        // Headers synchronised.
                        logger.DebugFormat("Block headers synchronised.");
                        //_getBlockHeadersSignal.Set();
                    }

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

        private void LoadBlockButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = _blockSearchText.Text;
            LoadBlockAsync(searchText);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentBlock = null;
            ViewModel.CurrentTransaction = null;
            ViewModel.Status = MainWindowViewModel.STATUS_READY_TEXT;
        }

        private void LoadPreviousBlockButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = _previousBlockText.Text;
            LoadBlockAsync(searchText);
        }

        private async void LoadBlockAsync(string searchText)
        {
            ViewModel.CurrentBlock = null;
            ViewModel.CurrentTransaction = null;

            var node = await InitialiseNode();

            if (node != null)
            {
                if (!String.IsNullOrEmpty(searchText))
                {
                    if (uint256.TryParse(searchText, out _requestedBlockHash))
                    {
                        ViewModel.Status = String.Format("Requesting block {0}...", _requestedBlockHash);

                        _waitForGetDataResponseSignal.Reset();
                        var dp = new GetDataPayload(new InventoryVector(InventoryType.MSG_BLOCK, _requestedBlockHash));

#pragma warning disable CS4014, CS1998
                        node.SendMessageAsync(dp);

                        Task.Run(async () =>
                        {
                            _waitForGetDataResponseSignal.Wait(WAIT_RESPONSE_TIMEOUT_MILLISECONDS, _cancellationTokenSource.Token);
                            if (_waitForGetDataResponseSignal.IsSet == false)
                            {
                                // No response to get data request.
                                App.Current.Dispatcher.Invoke(delegate
                                {
                                    ViewModel.Status = "No response received for requested block.";
                                });
                            }
                        });
#pragma warning restore
                    }
                    else
                    {
                        ViewModel.Status = "Block search string is an incorrect format.";
                    }
                }
                else
                {
                    ViewModel.Status = "Block search string is empty.";
                }
            }
        }

        private async void LoadHeadersButton_Click(object sender, RoutedEventArgs e)
        {
            var node = await InitialiseNode();

            if (node != null)
            {
                ViewModel.Status = "Requesting block headers...";

                var scanLocation = new BlockLocator();
                //scanLocation.Blocks.Add(ViewModel.BlockChainHeaders.Tip != null ? ViewModel.BlockChainHeaders.Tip.HashBlock : _network.GetGenesis().GetHash());
                scanLocation.Blocks.Add(_network.GetGenesis().GetHash());

                _waitForGeHeadersResponseSignal.Reset();
                var getHeadersPayload = new GetHeadersPayload(scanLocation);

#pragma warning disable CS4014, CS1998
                node.SendMessageAsync(getHeadersPayload);

                Task.Run(async () =>
                {
                    _waitForGeHeadersResponseSignal.Wait(WAIT_RESPONSE_TIMEOUT_MILLISECONDS, _cancellationTokenSource.Token);
                    if (_waitForGeHeadersResponseSignal.IsSet == false)
                    {
                    // No response to get data request.
                    App.Current.Dispatcher.Invoke(delegate
                        {
                            ViewModel.Status = "No response received for get headers request.";
                        });
                    }
                });
#pragma warning restore
            }
        }

        private void OnBlockHeaderList_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(sender is ListViewItem item && item.Content is DummyHeader dh)
            {
                _blockSearchText.Text = dh.BlockHash;
                LoadBlockAsync(dh.BlockHash);
            }
        }

        private void BlockArrow_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.CurrentBlock != null)
            {
                ViewModel.CurrentTransaction = ViewModel.CurrentBlock.Transactions.FirstOrDefault()?.ToString(ViewModel.BlockDisplayFormat);
                ViewModel.CurrentTransactionRaw = ViewModel.CurrentBlock.Transactions.FirstOrDefault()?.ToHex();
            }
        }

        private void BlockArrow_MouseLeftButtonUp_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.CurrentBlock != null)
            {
                ViewModel.CurrentTransaction = ViewModel.CurrentBlock.Transactions.LastOrDefault()?.ToString(ViewModel.BlockDisplayFormat);
                ViewModel.CurrentTransactionRaw = ViewModel.CurrentBlock.Transactions.LastOrDefault()?.ToHex();
            }
        }
    }
}