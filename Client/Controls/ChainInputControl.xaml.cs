using Client.EnumSpace;
using Client.ServiceSpace;
using Core.EnumSpace;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.ControlSpace
{
  public partial class ChainInputControl : UserControl
  {
    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register("Symbol", typeof(string), typeof(ChainInputControl), new PropertyMetadata(null));
    public static readonly DependencyProperty ExpirationProperty = DependencyProperty.Register("Expiration", typeof(string), typeof(ChainInputControl), new PropertyMetadata(null));

    /// <summary>
    /// Symbol
    /// </summary>
    public string Symbol
    {
      get => (string)GetValue(SymbolProperty);
      set => SetValue(SymbolProperty, value);
    }

    /// <summary>
    /// Expiration
    /// </summary>
    public string Expiration
    {
      get => (string)GetValue(ExpirationProperty);
      set => SetValue(ExpirationProperty, value);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public ChainInputControl()
    {
      InitializeComponent();
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
  }
}
