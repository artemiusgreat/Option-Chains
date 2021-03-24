using System.Windows;

namespace Client.WindowSpace
{
  /// <summary>
  /// Interaction logic
  /// </summary>
  public partial class MainWindow
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Close all child windows
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnWindowClose(object sender, System.EventArgs e)
    {
      foreach (Window o in Application.Current.Windows)
      {
        o.Close();
      }
    }
  }
}
