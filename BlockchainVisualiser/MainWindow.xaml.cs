using System;
using System.Windows;
using log4net;

namespace BlockchainVisualiser
{
    public partial class MainWindow : Window
    {
        private ILog logger = log4net.LogManager.GetLogger("default");

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

            root.DataContext = MainWindowViewModel.Instance;

            ViewModel.Status = MainWindowViewModel.STATUS_READY_TEXT;
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel?.CancellationSource.Cancel();
            ViewModel?.BtcClient?.Close();
            base.OnClosed(e);
        }
    }
}