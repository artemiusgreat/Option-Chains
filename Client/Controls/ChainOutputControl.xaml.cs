using Chart;
using Chart.ModelSpace;
using Chart.SeriesSpace;
using Client.ModelSpace;
using Client.ServiceSpace;
using Core.EnumSpace;
using Core.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.ControlSpace
{
  /// <summary>
  /// Control class
  /// </summary>
  public partial class ChainOutputControl : UserControl
  {
    private string _name = "Options";
    private IList<Chart.ModelSpace.IPointModel> _points = new List<Chart.ModelSpace.IPointModel>();

    public bool Variance { get; set; } = true;
    public IList<ChainInputModel> Chains { get; set; }
    public Func<IInstrumentOptionModel, bool> Where { get; set; } = item => true;
    public Func<IInstrumentOptionModel, double?, double?> Select { get; set; } = (items, price) => 0.0;

    /// <summary>
    /// Constructor
    /// </summary>
    public ChainOutputControl()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Render charts
    /// </summary>
    public void CreateCharts()
    {
      View.Composer = new ComponentComposer
      {
        Name = _name,
        Control = View,
        Group = new AreaModel(),
        ShowIndexAction = i =>
        {
          dynamic point = _points.ElementAtOrDefault((int)i);

          if (point == null)
          {
            return null;
          }

          return $"{ point.Strike }";
        }
      };

      View.Composer.Create();
      View.Composer.Update();

      var clock = new Timer();

      clock.Interval = 1000;
      clock.Enabled = true;
      clock.Elapsed += async (object sender, ElapsedEventArgs e) =>
      {
        clock.Stop();
        await OnData();
      };
    }

    /// <summary>
    /// Data updates
    /// </summary>
    private async Task OnData()
    {
      // Get price

      var instrument = (await DataService
        .GetInstruments(Chains.Select(o => o.Symbol).ToList()))
        .FirstOrDefault()
        .Value;

      if (instrument == null)
      {
        return;
      }

      // Get chains

      var optionQueries = Chains.Select(chain =>
      {
        var inputs = new Dictionary<dynamic, dynamic>
        {
          ["greeks"] = Variance,
          ["symbol"] = chain.Symbol,
          ["expiration"] = chain.Expiration
        };

        return DataService.GetOptionChain(inputs);
      });

      var options = (await Task
        .WhenAll(optionQueries))
        .SelectMany(o => o)
        .GroupBy(o => o.Strike)
        .ToDictionary(o => o.Key, o => o.Where(v => Where(v)).ToList())
        .OrderBy(o => o.Key)
        .ToList();

      // Create points with values for each expiration

      var currentPrice = instrument.Point.Price.Value;
      var sources = new Dictionary<string, ISeriesModel>();
      var points = options.Select(o =>
      {
        var strike = o.Key;
        var strikeOptions = o.Value;
        var series = new Dictionary<string, ISeriesModel>();

        // Create series based on option side and specified expirations

        foreach (var option in strikeOptions)
        {
          var seriesColor = Brushes.Black.Color;
          var seriesName = $"{option.Side} - {option.ExpirationDate:yyyy-MM-dd}";

          switch (option.Side.Value)
          {
            case OptionSideEnum.Put: seriesColor = Brushes.OrangeRed.Color; break;
            case OptionSideEnum.Call: seriesColor = Brushes.LimeGreen.Color; break;
          }

          sources[seriesName] = series[seriesName] = new SeriesModel
          {
            Name = seriesName,
            Shape = new BarSeries(),
            Model = new ExpandoModel
            {
              ["Color"] = seriesColor,
              ["Point"] = Select(option, currentPrice)
            }
          };
        }

        // Create point with all series

        var point = new Chart.ModelSpace.PointModel()
        {
          Areas = new Dictionary<string, IAreaModel>
          {
            [_name] = new AreaModel
            {
              Name = _name,
              Series = series
            }
          }
        };

        point["Strike"] = strike;

        return point as Chart.ModelSpace.IPointModel;

      }).ToList();

      // Set appropriate number of series on the chart 

      if (sources.Any())
      {
        View.Composer.Group.Series = sources;
      }

      await Dispatcher.BeginInvoke(new Action(() =>
      {
        var composer = View.Composer;

        composer.IndexDomain ??= new int[2];
        composer.IndexDomain[0] = 0;
        composer.IndexDomain[1] = options.Count;
        composer.Items = _points = points.ToList();
        composer.Update();
      }));
    }
  }
}
