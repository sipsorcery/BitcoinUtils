// Block Header format: https://bitcoin.org/en/developer-reference#block-headers

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NBitcoin;

namespace BlockchainVisualiser
{
    public class DummyHeader
    {
        public DummyHeader()
        { }

        private string _blockHash;
        public string BlockHash
        {
            get => _blockHash;
            set => _blockHash = value;
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public const string STATUS_READY_TEXT = "Ready";

        public event PropertyChangedEventHandler PropertyChanged;

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    _status = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
                }
            }
        }

        private ObservableCollection<DummyHeader> _blockHeaders = new ObservableCollection<DummyHeader>();
        public ObservableCollection<DummyHeader> BlockChainHeaders
        {
            get => _blockHeaders;
            set
            {
                value = _blockHeaders;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BlockChainHeaders"));
            }
        }

        //private List<BlockHeader> _blockHeaders = new List<BlockHeader>();
        //public List<BlockHeader> BlockChainHeaders
        //{
        //    get => _blockHeaders;
        //    set
        //    {
        //        value = _blockHeaders;
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BlockChainHeaders"));
        //    }
        //}

        //private ConcurrentChain _chain;
        //public ConcurrentChain BlockChainHeaders
        //{
        //    get => _chain;
        //    set
        //    {
        //        _chain = value;
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BlockChainHeaders"));
        //    }
        //}

        private Block _currentBlock;
        public Block CurrentBlock
        {
            get => _currentBlock;
            set
            {
                if(value != _currentBlock)
                {
                    _currentBlock = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeaderVersion"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeaderPreviousHash"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeaderMerkleRoot"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeaderBlockTime"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeaderBits"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HeaderNonce"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TransactionCount"));
                }
            }
        }

        public int? HeaderVersion
        {
            get => _currentBlock?.Header.Version;
        }

        public string HeaderPreviousHash
        {
            get => _currentBlock?.Header.HashPrevBlock.ToString();
        }

        public string HeaderMerkleRoot
        {
            get => _currentBlock?.Header.HashMerkleRoot.ToString();
        }

        public string HeaderBlockTime
        {
            get => _currentBlock?.Header.BlockTime.ToString("dd MMM yyyy HH:mm:ss zzz");
        }

        public string HeaderBits
        {
            get => _currentBlock?.Header.Bits.ToString();
        }

        public uint? HeaderNonce
        {
            get => _currentBlock?.Header.Nonce;
        }

        public int? TransactionCount
        {
            get => _currentBlock?.Transactions?.Count;
        }

        private string _currentTransaction;
        public string CurrentTransaction
        {
            get => _currentTransaction;
            set
            {
                if (value != _currentTransaction)
                {
                    _currentTransaction = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTransaction"));
                }
            }
        }

        private string _currentTransactionRaw;
        public string CurrentTransactionRaw
        {
            get => _currentTransactionRaw;
            set
            {
                if (value != _currentTransactionRaw)
                {
                    _currentTransactionRaw = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTransactionRaw"));
                }
            }
        }

        public bool DisplayFormat_BlockExplorer { get; set; }
        public bool DisplayFormat_Satoshi { get; set; }

        public RawFormat BlockDisplayFormat
        {
            get => DisplayFormat_Satoshi == true ? RawFormat.Satoshi : RawFormat.BlockExplorer;
        }
    }
}
