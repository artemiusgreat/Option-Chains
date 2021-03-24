using Client.EnumSpace;
using Client.ModelSpace;
using Client.ServiceSpace;
using Client.WindowSpace;
using Core.EnumSpace;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Client.ControlSpace
{
  public enum SymbolActionEnum : byte
  {
    None = 0,
    AskMinusBid = 1,
    OI = 2,
    OpenInterestMinusVolume = 3,
    IV = 4,
    Distribution = 5,
    Delta = 6,
    Gamma = 7,
    Vega = 8
  }

  public partial class ChainInputPanelControl : UserControl
  {
    /// <summary>
    /// Input controls
    /// </summary>
    public ObservableCollection<FrameworkElement> InputControls { get; } = new ObservableCollection<FrameworkElement>
    {
      new ChainInputControl
      {
        Margin = new Thickness(0, 0, 0, 15)
      }
    };

    /// <summary>
    /// Actions
    /// </summary>
    public ObservableCollection<string> SymbolActions { get; } = new ObservableCollection<string>
    {
      "Ask vs Bid",
      "Open Interest",
      "Volume vs Interest",
      "Implied Volatility",
      "Distribution",
      "Delta",
      "Gamma",
      "Vega",
      "Open Interest - Summary"
    };

    /// <summary>
    /// Constructor
    /// </summary>
    public ChainInputPanelControl()
    {
      InitializeComponent();

      ComboSymbolActions.ItemsSource = SymbolActions;
      SymbolInputsContainer.ItemsSource = InputControls;

      CommandService.Events.Subscribe((message) =>
      {
        if (Equals(message.Entity, EntityEnum.Chain))
        {
          var inputControl = message.Content as ChainInputControl;

          switch (message.Action)
          {
            case ActionEnum.Create:

              InputControls.Add(new ChainInputControl { Margin = new Thickness(0, 0, 0, 15) });
              break;

            case ActionEnum.Delete:

              if (InputControls.Count > 1)
              {
                InputControls.Remove(inputControl);
              }

              break;
          }
        }
      });
    }

    /// <summary>
    /// Add control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCreate(object sender, RoutedEventArgs e)
    {
      CommandService.Events.OnNext(new Message
      {
        Content = this,
        Entity = EntityEnum.Chain,
        Action = ActionEnum.Create
      });
    }

    /// <summary>
    /// Remove control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnRemove(object sender, RoutedEventArgs e)
    {
      CommandService.Events.OnNext(new Message
      {
        Content = this,
        Entity = EntityEnum.Chain,
        Action = ActionEnum.Delete
      });
    }

    /// <summary>
    /// Close input panel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSymbolCollapse(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (SymbolPanel.Visibility == Visibility.Collapsed)
      {
        SymbolCollapseControl.Kind = PackIconFontAwesomeKind.ChevronCircleDownSolid;
        SymbolPanel.Visibility = Visibility.Visible;
        return;
      }

      SymbolCollapseControl.Kind = PackIconFontAwesomeKind.ChevronCircleRightSolid;
      SymbolPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Event handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowChart(string caption, dynamic control)
    {
      var popup = new PopupWindow
      {
        Title = caption,
        Content = control
      };

      popup.Show();

      if (control is ChainOutputControl || control is CascadeOutputControl)
      {
        control.CreateCharts();
      }
    }

    /// <summary>
    /// Show popup with error message
    /// </summary>
    /// <param name="message"></param>
    private void ShowError(string message)
    {
      var popup = new PopupWindow
      {
        Title = "Error",
        Content = new Border
        {
          Padding = new Thickness(15, 15, 15, 15),
          Child = new TextBlock
          {
            Text = message
          }
        }
      };

      popup.Show();
    }

    /// <summary>
    /// Action selection
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSymbolActionSelection(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      var control = sender as ComboBox;
      var selection = $"{ control.SelectedItem }";

      if (control.SelectedIndex == -1)
      {
        return;
      }

      Dispatcher.BeginInvoke(new Action(() =>
      {
        control.Text = "Actions";
        control.SelectedIndex = -1;
      }));

      var chains = new List<ChainInputModel>();

      foreach (ChainInputControl item in SymbolInputsContainer.Items)
      {
        var symbolInput = item.Symbol;
        var expirationInput = item.Expiration;
        var isNoSymbol = string.IsNullOrEmpty(symbolInput);
        var isNoDate = DateTime.TryParseExact(expirationInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expirationDate) == false;

        if (isNoSymbol || isNoDate)
        {
          ShowError("Missing symbol name and date in format YYYY-MM-DD");
          return;
        }

        chains.Add(new ChainInputModel
        {
          Symbol = symbolInput,
          Expiration = expirationDate.ToString("yyyy-MM-dd")
        });
      }

      switch (selection)
      {
        case "Ask vs Bid":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = false,
            Chains = chains,
            Select = o => o.Point.BidSize - o.Point.AskSize,
            Where = o => o.Point.AskSize > 0 && o.Point.BidSize > 0
          });

          break;

        case "Open Interest":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = false,
            Chains = chains,
            Select = o => o.OpenInterest
          });

          break;

        case "Volume vs Interest":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = false,
            Chains = chains,
            Select = o => o.OpenInterest - o.Volume
          });

          break;

        case "Implied Volatility":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = true,
            Chains = chains,
            Select = o => o.Greeks.Iv
          });

          break;

        case "Distribution":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = true,
            Chains = chains,
            Select = o => o.Greeks.Distribution
          });

          break;

        case "Delta":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = true,
            Chains = chains,
            Select = o => o.Greeks.Delta
          });

          break;

        case "Gamma":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = true,
            Chains = chains,
            Select = o => o.Greeks.Gamma
          });

          break;

        case "Vega":

          ShowChart(selection, new ChainOutputControl
          {
            Variance = true,
            Chains = chains,
            Select = o => o.Greeks.Vega
          });

          break;

        case "Open Interest - Summary":

          ShowChart(selection, new CascadeOutputControl
          {
            Variance = true,
            Chains = chains,
            SelectMany = items => items.Where(o => Equals(o.Side, OptionSideEnum.Call)).Sum(o => o.OpenInterest)
          });

          break;

      }
    }
  }
}
