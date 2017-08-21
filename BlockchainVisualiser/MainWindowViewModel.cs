using System.ComponentModel;
using NBitcoin;

namespace BlockchainVisualiser
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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

        public string HeaderBlockTime
        {
            get => _currentBlock?.Header.BlockTime.ToString("ddd MMM yyyy HH:mm:ss");
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
    }
}
