using System.Windows;

namespace WhisperPete.Tray
{
    public partial class RecordingOverlay : Window
    {
        public RecordingOverlay()
        {
            InitializeComponent();
            
            // Position at top-center of screen
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = 50;
        }
    }
}
